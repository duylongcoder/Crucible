using System.Collections.Generic;

namespace Alchemy.Core
{
    /// <summary>
    /// Luật của một run 8 Mùa. Mỗi <see cref="ICommand"/> được xử lý ngay và trọn vẹn; Core không chờ View.
    /// </summary>
    public sealed class RunStateMachine
    {
        private readonly Brewer _brewer;
        private readonly bool _checkInvariants;

        public RunState State { get; }

        /// <param name="checkInvariants">Chạy <see cref="InvariantChecker"/> sau mỗi Command (debug build và test).</param>
        public RunStateMachine(RunDef def, ulong seed, bool checkInvariants = false)
        {
            Validate(def);
            _brewer = new Brewer(def.Brew, def.Items, def.Scrap);
            _checkInvariants = checkInvariants;

            State = new RunState(def, seed)
            {
                Reputation = def.StartReputation,
                Gold = def.StartGold,
                BagCapacity = def.BagCapacity,
                Grid = NewGrid(def),
            };
            foreach (var item in def.StarterKit)
                State.BagItems.Add(new Product(item));

            EnterDecree();
            Verify();
        }

        /// <summary>Tiếp tục từ state đã khôi phục (snapshot của Persistence).</summary>
        internal RunStateMachine(RunState restored, bool checkInvariants = false)
        {
            Validate(restored.Def);
            _brewer = new Brewer(restored.Def.Brew, restored.Def.Items, restored.Def.Scrap);
            _checkInvariants = checkInvariants;
            State = restored;
            Verify();
        }

        /// <summary>Mức khó định tính của đối thủ Mùa hiện tại (★1–5).</summary>
        public int OpponentStars => Forge.Stars(State);

        public CommandResult Execute(ICommand command)
        {
            var result = Apply(command);
            if (result.Accepted)
                Verify();
            return result;
        }

        private CommandResult Apply(ICommand command)
        {
            var s = State;
            if (s.Phase == RunPhase.RunEnd)
                return CommandResult.Reject("Run đã kết thúc.");

            switch (command)
            {
                case ContinueCommand _:
                    return Continue();
                case InspectCommand c:
                    return Inspect(c);
                case BuyCommand c when s.Phase == RunPhase.Market:
                    return Buy(c.OfferIndex);
                case SellCommand c when s.Phase == RunPhase.Market:
                    return Sell(c.BagIndex);
                case RerollCommand _ when s.Phase == RunPhase.Market:
                    return Reroll();
                case LeaveMarketCommand _ when s.Phase == RunPhase.Market:
                    s.Phase = RunPhase.Placing;
                    return CommandResult.Ok;
                case SkipBrewCommand _ when s.Phase == RunPhase.Market || s.Phase == RunPhase.Placing:
                    return SkipBrew();
                // Mọi Command sửa lưới chỉ hợp lệ ở Placing: Brewing khóa lưới (I16).
                case PlaceCommand c when s.Phase == RunPhase.Placing:
                    return Place(c);
                case UnplaceCommand c when s.Phase == RunPhase.Placing:
                    return Unplace(c.Row, c.Col);
                case MoveCommand c when s.Phase == RunPhase.Placing:
                    return Move(c);
                case BrewCommand _ when s.Phase == RunPhase.Placing:
                    return Brew();
                case OfferCommand c when s.Phase == RunPhase.Offering:
                    return Offer(c.BagIndex);
                case ChooseRewardCommand c when s.Phase == RunPhase.Spoils:
                    return ChooseReward(c.Index);
                case null:
                    return CommandResult.Reject("Command rỗng.");
                default:
                    return CommandResult.Reject("Không thể làm việc này lúc này.");
            }
        }

        private CommandResult Continue()
        {
            var s = State;
            switch (s.Phase)
            {
                case RunPhase.Decree:
                    EnterMarket();
                    return CommandResult.Ok;
                case RunPhase.Brewing:
                    EndTurn();
                    return CommandResult.Ok;
                case RunPhase.Workshop:
                    if (HasOfferable())
                        s.Phase = RunPhase.Offering;
                    else
                        ResolveBattle(null);
                    return CommandResult.Ok;
                case RunPhase.Battle:
                    AfterBattle();
                    return CommandResult.Ok;
                default:
                    return CommandResult.Reject("Chưa thể tiếp tục.");
            }
        }

        private CommandResult Buy(int offerIndex)
        {
            var s = State;
            if (offerIndex < 0 || offerIndex >= s.OfferList.Count || s.OfferList[offerIndex].Sold)
                return CommandResult.Reject("Không có món hàng này.");
            if (s.BagItems.Count >= s.BagCapacity)
                return CommandResult.Reject("Túi đã đầy.");

            var offer = s.OfferList[offerIndex];
            if (offer.Item.Kind == ItemKind.Catalyst && s.CatalystCount >= s.Def.CatalystSlots)
                return CommandResult.Reject($"Đã đủ {s.Def.CatalystSlots} Xúc tác; bán bớt để mua thêm.");
            if (s.Gold < offer.Price)
                return CommandResult.Reject("Không đủ vàng.");

            s.Gold -= offer.Price;
            offer.Sold = true;
            s.BagItems.Add(new Product(offer.Item));
            return CommandResult.Ok;
        }

        private CommandResult Sell(int bagIndex)
        {
            var s = State;
            if (!InBag(bagIndex))
                return CommandResult.Reject("Không có vật phẩm này trong Túi.");

            var product = s.BagItems[bagIndex];
            s.BagItems.RemoveAt(bagIndex);
            s.Gold += Market.SellPrice(s, product.Def);
            return CommandResult.Ok;
        }

        private CommandResult Reroll()
        {
            var s = State;
            var cost = Market.RerollCost(s);
            if (s.Gold < cost)
                return CommandResult.Reject("Không đủ vàng để đổi hàng.");

            s.Gold -= cost;
            s.RerollsThisTurn++;
            Market.RollOffers(s);
            return CommandResult.Ok;
        }

        private CommandResult Place(PlaceCommand c)
        {
            var s = State;
            if (!InBag(c.BagIndex))
                return CommandResult.Reject("Không có vật phẩm này trong Túi.");
            if (!InGrid(c.Row, c.Col))
                return CommandResult.Reject("Ô nằm ngoài lưới.");

            var product = s.BagItems[c.BagIndex];
            if (product.Def.Kind != ItemKind.Mineral && product.Def.Kind != ItemKind.Additive)
                return CommandResult.Reject("Chỉ khoáng và phụ gia được cho vào lò.");
            if (!s.Grid.Place(c.Row, c.Col, product))
                return CommandResult.Reject("Ô đã có vật phẩm hoặc vật phẩm không cho vào lò được.");

            s.BagItems.RemoveAt(c.BagIndex);
            return CommandResult.Ok;
        }

        private CommandResult Unplace(int row, int col)
        {
            if (!InGrid(row, col) || State.Grid[row, col] == null)
                return CommandResult.Reject("Ô trống.");

            // Đặt lên lưới đã rút khỏi Túi nên trả về luôn vừa.
            State.BagItems.Add(State.Grid.Remove(row, col));
            return CommandResult.Ok;
        }

        private CommandResult Move(MoveCommand c)
        {
            var grid = State.Grid;
            if (!InGrid(c.FromRow, c.FromCol) || !InGrid(c.ToRow, c.ToCol) || grid[c.FromRow, c.FromCol] == null)
                return CommandResult.Reject("Không có gì để di chuyển.");

            var moving = grid.Remove(c.FromRow, c.FromCol);
            var other = grid.Remove(c.ToRow, c.ToCol);
            grid.Place(c.ToRow, c.ToCol, moving);
            if (other != null)
                grid.Place(c.FromRow, c.FromCol, other);
            return CommandResult.Ok;
        }

        private CommandResult Brew()
        {
            var s = State;
            var result = _brewer.Brew(s.Grid, s.Rng, Catalysts());
            // Nung là xác nhận tiêu hao 100% vật phẩm trên lưới.
            s.Grid = NewGrid(s.Def);
            if (result.Product != null)
                s.BagItems.Add(result.Product);
            result.GoldGained = result.Effects.Gold;
            result.Created = ApplyEffects(result.Effects);

            s.LastBrew = result;
            s.BrewsUsed++;
            s.Phase = RunPhase.Brewing;
            return CommandResult.Ok;
        }

        private CommandResult SkipBrew()
        {
            var s = State;
            foreach (var cell in s.Grid.ToFeedSequence())
                if (cell != null)
                    s.BagItems.Add(cell);
            s.Grid = NewGrid(s.Def);
            s.LastBrew = null;
            s.BrewsUsed++;
            EndTurn();
            return CommandResult.Ok;
        }

        private CommandResult Inspect(InspectCommand c)
        {
            var s = State;
            if (s.Phase == RunPhase.Brewing)
                return CommandResult.Reject("Không soi được khi đang nung.");
            if (!InBag(c.LensIndex) || !InBag(c.TargetIndex) || c.LensIndex == c.TargetIndex)
                return CommandResult.Reject("Chọn một Kính soi và một thành phẩm trong Túi.");

            var lens = s.BagItems[c.LensIndex];
            var target = s.BagItems[c.TargetIndex];
            if (lens.Def.Kind != ItemKind.Lens)
                return CommandResult.Reject("Đó không phải Kính soi.");
            // Đã lộ hết dòng hoặc là phế phẩm / vật phẩm gốc → không soi, không tiêu hao Kính.
            if (!target.CanInspect)
                return CommandResult.Reject("Không còn gì để soi.");

            Inspector.Reveal(target, lens.Def.RevealLines, s.Rng[RngStream.Inspect]);
            s.BagItems.RemoveAt(c.LensIndex);
            return CommandResult.Ok;
        }

        private CommandResult Offer(int bagIndex)
        {
            var s = State;
            if (!InBag(bagIndex) || !s.BagItems[bagIndex].Def.IsBrewed)
                return CommandResult.Reject("Chỉ dâng được thành phẩm đạt chuẩn.");

            var product = s.BagItems[bagIndex];
            s.BagItems.RemoveAt(bagIndex);
            ResolveBattle(product);
            return CommandResult.Ok;
        }

        private CommandResult ChooseReward(int index)
        {
            var s = State;
            if (index < 0 || index >= s.RewardList.Count)
                return CommandResult.Reject("Không có lựa chọn này.");

            RewardOffer.Apply(s, s.RewardList[index]);
            s.RewardList.Clear();
            EnterDecree();
            return CommandResult.Ok;
        }

        private void EnterDecree()
        {
            var s = State;
            s.Season++;
            s.BrewsUsed = 0;
            s.LastBrew = null;
            s.LastBattle = null;
            s.Modifier = s.NextModifier;
            s.NextModifier = s.Season < s.Def.SeasonsPerRun ? SeasonModifiers.RollNext(s) : null;
            Market.RollPriceFactors(s);
            s.Phase = RunPhase.Decree;
        }

        private void EnterMarket()
        {
            var effects = new EffectContext();
            EffectRunner.Run(EffectTrigger.OnShopEnter, Catalysts(), null, null, State.Rng[RngStream.Chance], effects);
            ApplyEffects(effects);
            State.RerollsThisTurn = 0;
            Market.RollOffers(State);
            State.Phase = RunPhase.Market;
        }

        private void EndTurn()
        {
            var s = State;
            if (s.BrewsUsed < s.Def.BrewsPerSeason)
            {
                EnterMarket();
                return;
            }

            s.OfferList.Clear();
            s.Phase = RunPhase.Workshop;
        }

        /// <summary>Dâng (product null = không có gì để dâng → tự động Thua), Rèn, Trận (I21).</summary>
        private void ResolveBattle(Product product)
        {
            var s = State;
            var opponentPower = Forge.OpponentPower(s);
            BattleResult result;
            if (product == null)
            {
                result = new BattleResult(null, null, false, 0, opponentPower);
            }
            else
            {
                var equipment = Forge.Pick(s);
                var power = Forge.Power(product, equipment, s.Def.QualityWeight);
                result = new BattleResult(product.Def, equipment, power >= opponentPower, power, opponentPower);
            }

            s.LastBattle = result;
            if (result.Won)
            {
                s.Wins++;
            }
            else
            {
                s.Reputation--;
                if (s.Reputation <= 0)
                    s.Outcome = RunOutcome.Exiled;
            }

            s.Phase = RunPhase.Battle;
        }

        private void AfterBattle()
        {
            var s = State;
            if (s.Outcome == RunOutcome.Exiled)
            {
                s.Phase = RunPhase.RunEnd;
                return;
            }

            // Hoàn thành Mùa cuối còn Uy tín = thắng run, kể cả khi thua trận cuối.
            if (s.Season >= s.Def.SeasonsPerRun)
            {
                s.Outcome = RunOutcome.Won;
                s.Phase = RunPhase.RunEnd;
                return;
            }

            if (s.LastBattle.Won)
            {
                RewardOffer.Roll(s);
                if (s.RewardList.Count > 0)
                {
                    s.Phase = RunPhase.Spoils;
                    return;
                }
            }

            EnterDecree();
        }

        /// <summary>Vàng và nguyên liệu Xúc tác tạo ra; nguyên liệu bị chặn khi Túi đầy (I10).</summary>
        private List<Product> ApplyEffects(EffectContext effects)
        {
            var s = State;
            s.Gold += System.Math.Max(0, effects.Gold);
            var created = new List<Product>();
            foreach (var id in effects.Created)
            {
                var item = s.Def.FindItem(id);
                if (item == null || s.BagItems.Count >= s.BagCapacity)
                    continue;
                var product = new Product(item);
                s.BagItems.Add(product);
                created.Add(product);
            }

            return created;
        }

        private List<Product> Catalysts()
        {
            var list = new List<Product>();
            foreach (var p in State.BagItems)
                if (p.Def.Kind == ItemKind.Catalyst)
                    list.Add(p);
            return list;
        }

        private bool HasOfferable()
        {
            foreach (var p in State.BagItems)
                if (p.Def.IsBrewed)
                    return true;
            return false;
        }

        private bool InBag(int index) => index >= 0 && index < State.BagItems.Count;

        private bool InGrid(int row, int col) =>
            row >= 0 && row < State.Grid.Height && col >= 0 && col < State.Grid.Width;

        private static Grid NewGrid(RunDef def) => new Grid(def.Brew.GridWidth, def.Brew.GridHeight);

        /// <summary>Thiếu những thứ này thì run sẽ crash giữa chừng; báo ngay khi tạo run. Validator đầy đủ ở Content (M4).</summary>
        private static void Validate(RunDef def)
        {
            if (def == null)
                throw new System.ArgumentNullException(nameof(def));
            if (def.Brew == null || def.Scrap == null || def.Items == null || def.ShopPool == null || def.StarterKit == null
                || def.ShopTierWeights == null || def.Rewards == null || def.Modifiers == null || def.StarThresholds == null)
                throw new System.ArgumentException($"RunDef '{def.Id}' thiếu dữ liệu bắt buộc.");
            if (def.SeasonsPerRun < 1 || def.BrewsPerSeason < 1 || def.StartReputation < 1 || def.ShopMaxTier < 1
                || def.BagCapacity < 0 || def.CatalystSlots < 0 || def.MaxBagCapacity < def.BagCapacity || def.StarterKit.Count > def.BagCapacity
                || def.PriceFactorMinPercent < 0 || def.PriceFactorMaxPercent < def.PriceFactorMinPercent)
                throw new System.ArgumentException($"RunDef '{def.Id}' có tham số ngoài giới hạn.");
            if (def.Opponents == null || def.Opponents.Count < def.SeasonsPerRun)
                throw new System.ArgumentException($"RunDef '{def.Id}' cần {def.SeasonsPerRun} đối thủ.");
            if (def.Equipment == null || def.Equipment.Count == 0)
                throw new System.ArgumentException($"RunDef '{def.Id}' không có trang bị để rèn.");
        }

        private void Verify()
        {
            if (!_checkInvariants)
                return;
            var violation = InvariantChecker.Check(State);
            if (violation != null)
                throw new System.InvalidOperationException(violation);
        }
    }
}
