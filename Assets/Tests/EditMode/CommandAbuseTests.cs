using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Alchemy.Content;
using Alchemy.Core;
using NUnit.Framework;
using static Alchemy.Tests.EditMode.TestContent;

namespace Alchemy.Tests.EditMode
{
    /// <summary>
    /// Input xấu ở biên Presentation → Core: index rác, command null/lạ, sai phase, spam,
    /// và content thoái hóa. Lệnh bị từ chối không được đổi state (hash giữ nguyên).
    /// </summary>
    public class CommandAbuseTests
    {
        private static readonly int[] BadIndices = { -1, -100, int.MinValue, int.MaxValue, 999 };

        private static readonly RunPhase[] Phases =
        {
            RunPhase.Decree, RunPhase.Market, RunPhase.Placing, RunPhase.Brewing, RunPhase.Workshop,
            RunPhase.Offering, RunPhase.Battle, RunPhase.Spoils, RunPhase.RunEnd,
        };

        private sealed class UnknownCommand : ICommand
        {
        }

        /// <summary>Đưa run tới đúng phase, Túi có Steel đã có thuộc tính, 2 Kính soi và vàng.</summary>
        private static RunStateMachine At(RunPhase phase)
        {
            var m = new RunStateMachine(CreateRun(), 3, checkInvariants: true);
            m.State.Gold = 50;
            m.State.BagItems.Add(new Product(Steel, 6, 1, new[] { new StatLine(StatIds.Quality, 6), new StatLine("sharpness", 2) }));
            m.State.BagItems.Add(Raw(Lens));

            void Do(ICommand c) => Assert.That(m.Execute(c).Accepted, Is.True, $"chuẩn bị {phase}: {c.GetType().Name}");

            if (phase == RunPhase.Decree)
                return m;
            Do(new ContinueCommand());
            if (phase == RunPhase.Market)
                return m;
            Do(new LeaveMarketCommand());
            Do(new PlaceCommand(0, 0, 0));
            if (phase == RunPhase.Placing)
                return m;
            Do(new BrewCommand());
            if (phase == RunPhase.Brewing)
                return m;
            Do(new ContinueCommand());
            Do(new SkipBrewCommand());
            Do(new SkipBrewCommand());
            if (phase == RunPhase.Workshop)
                return m;
            Do(new ContinueCommand());
            if (phase == RunPhase.Offering)
                return m;
            Do(new OfferCommand(m.State.Bag.ToList().FindIndex(p => p.Def == Steel)));
            if (phase == RunPhase.Battle)
                return m;
            Do(new ContinueCommand());
            if (phase == RunPhase.Spoils)
                return m;

            // RunEnd: thua 3 Mùa liền.
            Do(new ChooseRewardCommand(0));
            while (m.State.Phase != RunPhase.RunEnd)
            {
                if (m.State.Phase == RunPhase.Decree) Do(new ContinueCommand());
                else if (m.State.Phase == RunPhase.Market || m.State.Phase == RunPhase.Placing) Do(new SkipBrewCommand());
                else if (m.State.Phase == RunPhase.Offering)
                    Do(new OfferCommand(m.State.Bag.ToList().FindIndex(p => p.Def.IsBrewed)));
                else Do(new ContinueCommand());
            }

            return m;
        }

        private static IEnumerable<ICommand> GarbageCommands()
        {
            yield return null;
            yield return new UnknownCommand();
            foreach (var i in BadIndices)
            {
                yield return new BuyCommand(i);
                yield return new SellCommand(i);
                yield return new PlaceCommand(i, 0, 0);
                yield return new PlaceCommand(0, i, 0);
                yield return new PlaceCommand(0, 0, i);
                yield return new UnplaceCommand(i, i);
                yield return new MoveCommand(i, 0, 0, 0);
                yield return new MoveCommand(0, 0, i, 0);
                yield return new InspectCommand(i, 0);
                yield return new InspectCommand(0, i);
                yield return new OfferCommand(i);
                yield return new ChooseRewardCommand(i);
            }
        }

        [Test]
        public void GarbageCommand_InEveryPhase_RejectedWithoutChangingState([ValueSource(nameof(Phases))] RunPhase phase)
        {
            var m = At(phase);
            Assert.That(m.State.Phase, Is.EqualTo(phase));

            foreach (var command in GarbageCommands())
            {
                var before = m.State.ComputeHash();
                CommandResult result = null;
                Assert.DoesNotThrow(() => result = m.Execute(command), command?.GetType().Name ?? "null");
                Assert.That(result.Accepted, Is.False, $"{phase}: {Describe(command)} lẽ ra phải bị từ chối");
                Assert.That(result.Error, Is.Not.Empty);
                Assert.That(m.State.ComputeHash(), Is.EqualTo(before), $"{phase}: {Describe(command)} đổi state dù bị từ chối");
            }
        }

        // Lệnh hợp lệ về tham số nhưng gửi sai phase.
        private static readonly (ICommand command, RunPhase[] allowed)[] Matrix =
        {
            (new ContinueCommand(), new[] { RunPhase.Decree, RunPhase.Brewing, RunPhase.Workshop, RunPhase.Battle }),
            (new BuyCommand(0), new[] { RunPhase.Market }),
            (new SellCommand(0), new[] { RunPhase.Market }),
            (new RerollCommand(), new[] { RunPhase.Market }),
            (new LeaveMarketCommand(), new[] { RunPhase.Market }),
            (new SkipBrewCommand(), new[] { RunPhase.Market, RunPhase.Placing }),
            (new PlaceCommand(0, 1, 1), new[] { RunPhase.Placing }),
            (new UnplaceCommand(0, 0), new[] { RunPhase.Placing }),
            (new MoveCommand(0, 0, 2, 2), new[] { RunPhase.Placing }),
            (new BrewCommand(), new[] { RunPhase.Placing }),
            (new OfferCommand(0), new[] { RunPhase.Offering }),
            (new ChooseRewardCommand(0), new[] { RunPhase.Spoils }),
        };

        [Test]
        public void Command_InWrongPhase_RejectedWithoutChangingState([ValueSource(nameof(Phases))] RunPhase phase)
        {
            foreach (var (command, allowed) in Matrix)
            {
                if (allowed.Contains(phase))
                    continue;
                var m = At(phase);
                var before = m.State.ComputeHash();

                Assert.That(m.Execute(command).Accepted, Is.False, $"{command.GetType().Name} được chấp nhận ở {phase}");
                Assert.That(m.State.ComputeHash(), Is.EqualTo(before));
            }
        }

        [Test]
        public void Inspect_AllowedEverywhereExceptBrewingAndRunEnd([ValueSource(nameof(Phases))] RunPhase phase)
        {
            var m = At(phase);
            // Ở Battle/Spoils/RunEnd, Steel đã bị dâng trong lúc chuẩn bị: thêm lại để chỉ còn phase là biến số.
            m.State.BagCapacity = m.State.Def.MaxBagCapacity;
            m.State.BagItems.Add(new Product(Steel, 6, 1, new[] { new StatLine(StatIds.Quality, 6) }));
            m.State.BagItems.Add(Raw(Lens));
            var steel = m.State.Bag.Count - 2;
            var lens = m.State.Bag.Count - 1;

            var accepted = m.Execute(new InspectCommand(lens, steel)).Accepted;

            Assert.That(accepted, Is.EqualTo(phase != RunPhase.Brewing && phase != RunPhase.RunEnd));
        }

        [Test]
        public void Spam_Continue_NeverSkipsBrewTurns()
        {
            var m = At(RunPhase.Decree);
            for (var i = 0; i < 100; i++)
                m.Execute(new ContinueCommand());

            Assert.That(m.State.Phase, Is.EqualTo(RunPhase.Market));
            Assert.That(m.State.BrewsUsed, Is.EqualTo(0));
        }

        [Test]
        public void Spam_Brew_BrewsOnceAndConsumesOnce()
        {
            var m = At(RunPhase.Placing);
            var bagBefore = m.State.Bag.Count;
            for (var i = 0; i < 100; i++)
                m.Execute(new BrewCommand());

            Assert.That(m.State.BrewsUsed, Is.EqualTo(1));
            Assert.That(m.State.Bag.Count, Is.EqualTo(bagBefore + 1));
        }

        [Test]
        public void Spam_Buy_StopsAtGoldOrBag_GoldNeverNegative()
        {
            var m = At(RunPhase.Market);
            for (var i = 0; i < 200; i++)
                m.Execute(new BuyCommand(0));

            Assert.That(m.State.Gold, Is.GreaterThanOrEqualTo(0));
            Assert.That(m.State.Bag.Count, Is.LessThanOrEqualTo(m.State.BagCapacity));
        }

        [Test]
        public void Spam_Reroll_CostKeepsRising_GoldNeverNegative()
        {
            var m = At(RunPhase.Market);
            var rerolls = 0;
            for (var i = 0; i < 100; i++)
                if (m.Execute(new RerollCommand()).Accepted)
                    rerolls++;

            // 50 vàng: 2 + 3 + … + 9 = 44 (8 lần), lần thứ 9 cần 10 vàng nhưng chỉ còn 6.
            Assert.That(rerolls, Is.EqualTo(8));
            Assert.That(m.State.Gold, Is.EqualTo(6));
        }

        [Test]
        public void Spam_Offer_ConsumesOnlyOneProductPerSeason()
        {
            var m = At(RunPhase.Offering);
            m.State.BagItems.Add(new Product(Steel, 6, 1, new[] { new StatLine(StatIds.Quality, 6) }));
            var brewed = m.State.Bag.Count(p => p.Def.IsBrewed);

            for (var i = 0; i < 20; i++)
                m.Execute(new OfferCommand(m.State.Bag.ToList().FindIndex(p => p.Def.IsBrewed)));

            Assert.That(m.State.Bag.Count(p => p.Def.IsBrewed), Is.EqualTo(brewed - 1));
            Assert.That(m.State.Phase, Is.EqualTo(RunPhase.Battle));
        }

        [Test]
        public void Spam_ChooseReward_AppliesOnlyOne()
        {
            var m = At(RunPhase.Spoils);
            var capacity = m.State.BagCapacity;
            var bag = m.State.Bag.Count;

            for (var i = 0; i < 20; i++)
                m.Execute(new ChooseRewardCommand(i % 3));

            var gained = (m.State.BagCapacity - capacity) + (m.State.Bag.Count - bag);
            Assert.That(gained, Is.EqualTo(2).Or.EqualTo(1), "Nới Túi +2 hoặc 1 khoáng hiếm, không cộng dồn");
            Assert.That(m.State.Season, Is.EqualTo(2));
        }

        [Test]
        public void Spam_Inspect_ConsumesLensOnlyWhenSomethingRevealed()
        {
            var m = At(RunPhase.Workshop);
            for (var i = 0; i < 3; i++)
                m.State.BagItems.Add(Raw(Lens));
            var lensesBefore = m.State.Bag.Count(p => p.Def == Lens);

            for (var i = 0; i < 50; i++)
            {
                var steel = m.State.Bag.ToList().FindIndex(p => p.Def == Steel);
                var lens = m.State.Bag.ToList().FindIndex(p => p.Def == Lens);
                if (lens < 0)
                    break;
                m.Execute(new InspectCommand(lens, steel));
            }

            // Steel có 2 dòng, Kính lộ 1 dòng/lần → đúng 2 Kính bị dùng.
            Assert.That(m.State.Bag.Count(p => p.Def == Lens), Is.EqualTo(lensesBefore - 2));
            Assert.That(m.State.Bag.First(p => p.Def == Steel).RevealedStats.Count, Is.EqualTo(2));
        }

        [Test]
        public void Move_ToSameCell_KeepsItem()
        {
            var m = At(RunPhase.Placing);
            var item = m.State.Grid[0, 0];

            Assert.That(m.Execute(new MoveCommand(0, 0, 0, 0)).Accepted, Is.True);
            Assert.That(m.State.Grid[0, 0], Is.SameAs(item));
        }

        [Test]
        public void Move_ToOccupiedCell_Swaps()
        {
            var m = At(RunPhase.Placing);
            Assert.That(m.Execute(new PlaceCommand(0, 2, 2)).Accepted, Is.True);
            var a = m.State.Grid[0, 0];
            var b = m.State.Grid[2, 2];

            Assert.That(m.Execute(new MoveCommand(0, 0, 2, 2)).Accepted, Is.True);
            Assert.That(m.State.Grid[0, 0], Is.SameAs(b));
            Assert.That(m.State.Grid[2, 2], Is.SameAs(a));
        }

        [Test]
        public void Replay_SameSeedSameCommands_SameHashAfterEveryCommand()
        {
            var def = DemoContent.CreateRun();
            var a = new RunStateMachine(def, 77, checkInvariants: true);
            var b = new RunStateMachine(def, 77, checkInvariants: true);
            var r = new Random(77);

            for (var i = 0; i < 2000 && a.State.Phase != RunPhase.RunEnd; i++)
            {
                ICommand c;
                switch (r.Next(8))
                {
                    case 0: c = new ContinueCommand(); break;
                    case 1: c = new LeaveMarketCommand(); break;
                    case 2: c = new PlaceCommand(r.Next(15), r.Next(3), r.Next(3)); break;
                    case 3: c = new BrewCommand(); break;
                    case 4: c = new SkipBrewCommand(); break;
                    case 5: c = new OfferCommand(r.Next(15)); break;
                    case 6: c = new ChooseRewardCommand(r.Next(3)); break;
                    default: c = new SellCommand(r.Next(15)); break;
                }

                Assert.That(b.Execute(c).Accepted, Is.EqualTo(a.Execute(c).Accepted));
                Assert.That(b.State.ComputeHash(), Is.EqualTo(a.State.ComputeHash()), $"lệch ở command {i}");
            }
        }

        // ---- Content thoái hóa: run không được treo hay ném exception giữa chừng ----

        private static readonly Func<RunDef, RunDef>[] DegenerateContent =
        {
            d => { d.ShopPool = new ItemDef[0]; return d; },
            d => { d.Rewards = new RewardDef[0]; return d; },
            d => { d.StarterKit = new ItemDef[0]; return d; },
            d => { d.ShopTierWeights = new[] { 0, 0, 0 }; return d; },
            d => { d.ShopTierWeights = new[] { 100 }; return d; },
            d => { d.Equipment = new[] { new EquipmentDef("zero", "Gậy", 0, new StatWeight[0]) }; return d; },
            d => { d.Opponents = d.Opponents.Select(o => new OpponentDef(o.Id, o.Name, o.Kind, double.NaN)).ToArray(); return d; },
            d => { d.Opponents = d.Opponents.Select(o => new OpponentDef(o.Id, o.Name, o.Kind, double.PositiveInfinity)).ToArray(); return d; },
        };

        [Test]
        public void DegenerateContent_RunStillReachesRunEnd([Range(0, 7)] int variant)
        {
            var def = DegenerateContent[variant](CreateRun());
            var m = new RunStateMachine(def, 5, checkInvariants: true);
            for (var i = 0; i < 500 && m.State.Phase != RunPhase.RunEnd; i++)
            {
                var p = m.State.Phase;
                ICommand c = p == RunPhase.Market || p == RunPhase.Placing ? new SkipBrewCommand()
                    : p == RunPhase.Spoils ? new ChooseRewardCommand(0)
                    : p == RunPhase.Offering ? new OfferCommand(m.State.Bag.ToList().FindIndex(x => x.Def.IsBrewed))
                    : (ICommand)new ContinueCommand();
                m.Execute(c);
            }

            Assert.That(m.State.Phase, Is.EqualTo(RunPhase.RunEnd));
        }

        [TestCase(int.MaxValue)]
        [TestCase(-50)]
        [TestCase(0)]
        public void ExtremeMarketPrice_BuyAtLeastOne_SellAtLeastZero(int price)
        {
            var item = new ItemDef("x", ItemKind.Mineral, 1, marketPrice: price);
            var s = new RunStateMachine(CreateRun(), 1).State;
            s.Modifier = new SeasonModifierDef("m", "m", "", new[]
            {
                new ParamScale(ScaledParam.BuyPrice, 300), new ParamScale(ScaledParam.SellPrice, 300),
            });

            Assert.That(Market.BuyPrice(s, item), Is.GreaterThanOrEqualTo(1));
            Assert.That(Market.SellPrice(s, item), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void ExtremeRecipeValues_ProductStaysFinite()
        {
            var weird = new ItemDef("weird", ItemKind.Mineral, 4, new Recipe(
                new[] { new RecipeSlot("iron"), new RecipeSlot("coal") }, double.PositiveInfinity,
                stats: new[] { new RecipeStat("a", 100, double.NaN), new RecipeStat("b", 100, double.NegativeInfinity), new RecipeStat("c", 250, 1) }));
            var brewer = new Brewer(Rules, new[] { weird }, Scrap);

            var product = brewer.Brew(MakeGrid(Raw(Iron), Raw(Coal)), new RngStreams(1)).Product;

            Assert.That(InvariantChecker.CheckProduct(product), Is.Null);
        }

        [Test]
        public void MissingEquipmentOrOpponents_FailsFastAtConstruction()
        {
            var noEquipment = CreateRun();
            noEquipment.Equipment = new EquipmentDef[0];
            var fewOpponents = CreateRun();
            fewOpponents.Opponents = fewOpponents.Opponents.Take(3).ToArray();

            Assert.Throws<ArgumentException>(() => new RunStateMachine(noEquipment, 1));
            Assert.Throws<ArgumentException>(() => new RunStateMachine(fewOpponents, 1));
        }

        // ---- Không rò rỉ thông tin qua API public ----

        [TestCase(typeof(Product))]
        [TestCase(typeof(Recipe))]
        [TestCase(typeof(RecipeSlot))]
        [TestCase(typeof(RecipeStat))]
        [TestCase(typeof(OpponentDef))]
        [TestCase(typeof(EquipmentDef))]
        [TestCase(typeof(StatWeight))]
        [TestCase(typeof(BattleResult))]
        [TestCase(typeof(RunDef))]
        [TestCase(typeof(RunStateMachine))]
        [TestCase(typeof(RunState))]
        public void PublicApi_DoesNotExposeHiddenValues(Type type)
        {
            var hidden = new[]
            {
                "Power", "OpponentPower", "Stats", "Essence", "Potency", "Quality", "Pattern", "AllowedAdditives",
                "BaseEssence", "StatWeights", "PoolWeight", "QualityWeight", "StarThresholds", "IsMagic", "ItemId",
                "RequiresMagic", "ChancePercent", "Value", "Weight", "Rng", "PriceFactors",
            };
            var exposed = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => m is PropertyInfo || m is FieldInfo || m is MethodInfo)
                .Select(m => m.Name)
                .Intersect(hidden)
                .ToArray();

            Assert.That(exposed, Is.Empty);
        }

        [Test]
        public void HiddenTypes_NotPublic()
        {
            Assert.That(typeof(RecipeMatcher).IsPublic, Is.False);
            Assert.That(typeof(Brewer).IsPublic, Is.False);
            Assert.That(typeof(ProductFactory).IsPublic, Is.False);
            Assert.That(typeof(Forge).IsPublic, Is.False);
            Assert.That(typeof(Inspector).IsPublic, Is.False);
        }

        [Test]
        public void BrewLog_FuzzRandomGrids_OnlyTickEventsThenOneResult()
        {
            var pool = new[] { Iron, Coal, Copper, Flux, Salt, Sulfur, Steel, Bronze };
            var r = new Random(9);
            for (var n = 0; n < 2000; n++)
            {
                var cells = new Product[9];
                for (var i = 0; i < 9; i++)
                    if (r.Next(3) == 0)
                        cells[i] = r.Next(4) == 0 ? Magic(pool[r.Next(pool.Length)]) : Raw(pool[r.Next(pool.Length)]);

                var result = Brew((ulong)n, cells);
                var events = result.Log.Events;
                var filled = Enumerable.Range(0, 9).Where(i => cells[i] != null).ToArray();
                if (filled.Length == 0)
                {
                    Assert.That(events, Is.Empty);
                    continue;
                }

                Assert.That(events.Count, Is.EqualTo(filled.Last() - filled.First() + 2));
                for (var k = 0; k < events.Count - 1; k++)
                {
                    var e = events[k];
                    Assert.That(e.Tick, Is.EqualTo(filled.First() + k));
                    Assert.That(e.Kind, Is.EqualTo(cells[e.Tick] == null ? BrewEventKind.Wait : BrewEventKind.Feed));
                    Assert.That(e.ItemId, Is.EqualTo(cells[e.Tick]?.Def.Id));
                }

                Assert.That(events.Last().Kind, Is.EqualTo(BrewEventKind.Result));
                Assert.That(events.Last().ItemId, Is.EqualTo(result.Product.Def.Id));
                Assert.That(InvariantChecker.CheckProduct(result.Product), Is.Null);
            }
        }

        private static string Describe(ICommand c) => c == null ? "null" : c.GetType().Name;
    }
}
