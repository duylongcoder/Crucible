using System.Collections.Generic;

namespace Alchemy.Core
{
    public enum RunPhase
    {
        Decree,
        Market,
        Placing,
        Brewing,
        Workshop,
        Offering,
        Battle,
        Spoils,
        RunEnd,
    }

    public enum RunOutcome
    {
        None,
        Won,
        Exiled,
    }

    public sealed class ShopOffer
    {
        public ItemDef Item { get; }
        public int Price { get; }

        /// <summary>Món đã mua giữ nguyên chỗ để vị trí các món khác không xô lệch (click đúp không mua nhầm).</summary>
        public bool Sold { get; internal set; }

        public ShopOffer(ItemDef item, int price)
        {
            Item = item;
            Price = price;
        }
    }

    /// <summary>Kết quả trận: Presentation chỉ thấy trang bị và Thắng/Thua; Power là internal.</summary>
    public sealed class BattleResult
    {
        /// <summary>Null khi không có gì để dâng (tự động Thua).</summary>
        public ItemDef Offered { get; }
        public EquipmentDef Equipment { get; }
        public bool Won { get; }
        internal double Power { get; }
        internal double OpponentPower { get; }

        internal BattleResult(ItemDef offered, EquipmentDef equipment, bool won, double power, double opponentPower)
        {
            Offered = offered;
            Equipment = equipment;
            Won = won;
            Power = power;
            OpponentPower = opponentPower;
        }
    }

    /// <summary>Toàn bộ trạng thái một run. Chỉ <see cref="RunStateMachine"/> được sửa.</summary>
    public sealed class RunState
    {
        internal readonly List<Product> BagItems = new List<Product>();
        internal readonly List<ShopOffer> OfferList = new List<ShopOffer>();
        internal readonly List<RewardDef> RewardList = new List<RewardDef>();

        public RunDef Def { get; }
        public ulong Seed { get; }
        internal RngStreams Rng { get; }

        public int Season { get; internal set; }
        public RunPhase Phase { get; internal set; }
        public int BrewsUsed { get; internal set; }
        public int Reputation { get; internal set; }
        public int Gold { get; internal set; }
        public int Wins { get; internal set; }
        public RunOutcome Outcome { get; internal set; }
        public int BagCapacity { get; internal set; }
        public Grid Grid { get; internal set; }
        public int RerollsThisTurn { get; internal set; }

        /// <summary>Modifier của Mùa hiện tại và Mùa sau (biết trước 1 Mùa). Null = không có.</summary>
        public SeasonModifierDef Modifier { get; internal set; }
        public SeasonModifierDef NextModifier { get; internal set; }

        /// <summary>Hệ số giá Mùa (%), theo thứ tự <see cref="RunDef.Items"/>.</summary>
        internal int[] PriceFactors { get; set; }

        /// <summary>Lượt nung vừa xong (để Presentation phát lại); null nếu lượt bỏ qua.</summary>
        public BrewResult LastBrew { get; internal set; }
        public BattleResult LastBattle { get; internal set; }

        public IReadOnlyList<Product> Bag => BagItems;
        public IReadOnlyList<ShopOffer> Offers => OfferList;
        public IReadOnlyList<RewardDef> RewardChoices => RewardList;

        public OpponentDef Opponent => Season >= 1 && Season <= Def.Opponents.Count ? Def.Opponents[Season - 1] : null;

        public int CatalystCount
        {
            get
            {
                var n = 0;
                foreach (var p in BagItems)
                    if (p.Def.Kind == ItemKind.Catalyst)
                        n++;
                return n;
            }
        }

        internal RunState(RunDef def, ulong seed)
        {
            Def = def;
            Seed = seed;
            Rng = new RngStreams(seed);
        }

        public ulong ComputeHash()
        {
            var h = new StateHash();
            h.Add(Def.Id);
            h.Add(Seed);
            h.Add(Season);
            h.Add((int)Phase);
            h.Add(BrewsUsed);
            h.Add(Reputation);
            h.Add(Gold);
            h.Add(Wins);
            h.Add((int)Outcome);
            h.Add(BagCapacity);
            h.Add(RerollsThisTurn);
            h.Add(Modifier?.Id);
            h.Add(NextModifier?.Id);

            h.Add(BagItems.Count);
            foreach (var p in BagItems)
                p.AddTo(h);

            foreach (var cell in Grid.ToFeedSequence())
                if (cell == null)
                    h.Add(-1);
                else
                    cell.AddTo(h);

            h.Add(OfferList.Count);
            foreach (var o in OfferList)
            {
                h.Add(o.Item.Id);
                h.Add(o.Price);
                h.Add(o.Sold);
            }

            h.Add(RewardList.Count);
            foreach (var r in RewardList)
                h.Add(r.Id);

            h.Add(PriceFactors?.Length ?? 0);
            if (PriceFactors != null)
                foreach (var f in PriceFactors)
                    h.Add(f);

            h.Add(LastBattle?.Equipment?.Id);
            h.Add(LastBattle?.Won ?? false);
            Rng.AddTo(h);
            return h.Value;
        }
    }
}
