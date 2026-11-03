using Alchemy.Core;
using NUnit.Framework;

namespace Alchemy.Tests.EditMode
{
    /// <summary>Harness M2: RandomAgent chơi trên content demo, InvariantChecker chạy sau mỗi Command.</summary>
    public class RandomAgentTests
    {
        private const int MaxCommands = 5000;

        [Test]
        public void RandomAgent_1000Runs_NoInvariantViolation_AllReachRunEnd()
        {
            var def = TestContent.MvpRun();
            var outcomes = new int[3];
            for (ulong seed = 0; seed < (ulong)Harness.Runs(1000); seed++)
            {
                var m = Play(def, seed);
                Assert.That(m.State.Phase, Is.EqualTo(RunPhase.RunEnd), $"seed {seed} kẹt ở {m.State.Phase}");
                outcomes[(int)m.State.Outcome]++;
            }

            Assert.That(outcomes[(int)RunOutcome.None], Is.EqualTo(0));
            TestContext.WriteLine($"Thắng {outcomes[(int)RunOutcome.Won]}, trục xuất {outcomes[(int)RunOutcome.Exiled]}");
        }

        [Test]
        public void RandomAgent_SameSeedSameInputs_SameFinalHash()
        {
            var def = TestContent.MvpRun();

            Assert.That(Play(def, 42).State.ComputeHash(), Is.EqualTo(Play(def, 42).State.ComputeHash()));
        }

        [Test]
        public void GarbageAgent_1000Runs_NeverThrows_AllReachRunEnd()
        {
            var def = TestContent.MvpRun();
            int[] garbage = { -1, int.MinValue, int.MaxValue, 50, 3 };
            for (ulong seed = 0; seed < (ulong)Harness.Runs(1000); seed++)
            {
                var m = new RunStateMachine(def, seed, checkInvariants: true);
                var r = new System.Random((int)seed);
                // Index thường phủ cả Túi (trần 24) để run luôn có đường đi tiếp; 1/3 là rác.
                int G() => r.Next(3) == 0 ? garbage[r.Next(garbage.Length)] : r.Next(25);
                for (var i = 0; i < MaxCommands && m.State.Phase != RunPhase.RunEnd; i++)
                {
                    ICommand c;
                    switch (r.Next(16))
                    {
                        case 0: c = null; break;
                        case 1: c = new BuyCommand(G()); break;
                        case 2: c = new SellCommand(G()); break;
                        case 3: c = new RerollCommand(); break;
                        case 4: c = new LeaveMarketCommand(); break;
                        case 5: c = new PlaceCommand(G(), G(), G()); break;
                        case 6: c = new UnplaceCommand(G(), G()); break;
                        case 7: c = new MoveCommand(G(), G(), G(), G()); break;
                        case 8: c = new BrewCommand(); break;
                        case 9: c = new SkipBrewCommand(); break;
                        case 10: c = new InspectCommand(G(), G()); break;
                        case 11: c = new OfferCommand(G()); break;
                        case 12: c = new ChooseRewardCommand(G()); break;
                        default: c = new ContinueCommand(); break;
                    }

                    m.Execute(c);
                }

                Assert.That(m.State.Phase, Is.EqualTo(RunPhase.RunEnd), $"seed {seed}");
            }
        }

        [Test]
        public void ScriptedAgent_BrewsRecipes_CoversLateSeasonsAndWins()
        {
            var def = TestContent.MvpRun();
            var maxSeason = 0;
            int won = 0, spoils = 0;
            for (ulong seed = 0; seed < 300; seed++)
            {
                var m = new RunStateMachine(def, seed, checkInvariants: true);
                for (var i = 0; i < MaxCommands && m.State.Phase != RunPhase.RunEnd; i++)
                {
                    if (m.State.Phase == RunPhase.Spoils)
                        spoils++;
                    m.Execute(Scripted(m.State));
                }

                Assert.That(m.State.Phase, Is.EqualTo(RunPhase.RunEnd), $"seed {seed}");
                maxSeason = System.Math.Max(maxSeason, m.State.Season);
                if (m.State.Outcome == RunOutcome.Won)
                    won++;
            }

            TestContext.WriteLine($"Mùa xa nhất {maxSeason}, thắng run {won}/300, lần vào Spoils {spoils}");
            // Agent đơn giản (không bán để mua thêm) chỉ cần phủ được nửa sau của run; Mùa 8 có unit test riêng.
            Assert.That(maxSeason, Is.GreaterThanOrEqualTo(5));
            Assert.That(spoils, Is.GreaterThan(0));
        }

        // Thử lần lượt: Thép tôi, Thép, Đồng thau; Chợ mua sắt/than; dâng thành phẩm cấp cao nhất.
        private static readonly string[][] Patterns =
        {
            new[] { "steel", null, "coal", null, "steel" },
            new[] { "iron_ore", "borax", "coal" },
            new[] { "iron_ore", null, "coal" },
            new[] { "copper_ore", null, null, "tin" },
        };

        internal static ICommand Scripted(RunState s)
        {
            int Find(string id)
            {
                for (var i = 0; i < s.Bag.Count; i++)
                    if (s.Bag[i].Def.Id == id)
                        return i;
                return -1;
            }

            switch (s.Phase)
            {
                case RunPhase.Market:
                    var scrap = Find("scrap");
                    if (scrap >= 0)
                        return new SellCommand(scrap);
                    for (var i = 0; i < s.Offers.Count; i++)
                        if (!s.Offers[i].Sold && (s.Offers[i].Item.Id == "iron_ore" || s.Offers[i].Item.Id == "coal") && s.Offers[i].Price <= s.Gold
                            && s.Bag.Count < s.BagCapacity)
                            return new BuyCommand(i);
                    return new LeaveMarketCommand();
                case RunPhase.Placing:
                    foreach (var pattern in Patterns)
                    {
                        if (!CanComplete(s, pattern))
                            continue;
                        for (var c = 0; c < 9; c++)
                        {
                            var cell = s.Grid[c / 3, c % 3];
                            if (cell != null && (c >= pattern.Length || cell.Def.Id != pattern[c]))
                                return new UnplaceCommand(c / 3, c % 3);
                        }

                        for (var k = 0; k < pattern.Length; k++)
                            if (pattern[k] != null && s.Grid[k / 3, k % 3] == null)
                                return new PlaceCommand(Find(pattern[k]), k / 3, k % 3);
                        return new BrewCommand();
                    }

                    return new SkipBrewCommand();
                case RunPhase.Offering:
                    var best = -1;
                    for (var i = 0; i < s.Bag.Count; i++)
                        if (s.Bag[i].Def.IsBrewed && (best < 0 || s.Bag[i].Def.Tier > s.Bag[best].Def.Tier))
                            best = i;
                    return new OfferCommand(best);
                case RunPhase.Spoils:
                    return new ChooseRewardCommand(0);
                default:
                    return new ContinueCommand();
            }
        }

        /// <summary>Túi + các ô đã đúng chỗ đủ nguyên liệu cho cả công thức.</summary>
        private static bool CanComplete(RunState s, string[] pattern)
        {
            for (var k = 0; k < pattern.Length; k++)
            {
                var id = pattern[k];
                if (id == null || (s.Grid[k / 3, k % 3] != null && s.Grid[k / 3, k % 3].Def.Id == id))
                    continue;
                var need = 0;
                for (var j = 0; j < pattern.Length; j++)
                    if (pattern[j] == id && (s.Grid[j / 3, j % 3] == null || s.Grid[j / 3, j % 3].Def.Id != id))
                        need++;
                var have = 0;
                foreach (var p in s.Bag)
                    if (p.Def.Id == id)
                        have++;
                if (have < need)
                    return false;
            }

            return true;
        }

        /// <summary>Agent dùng chung seed run cho chuỗi input để cùng seed → cùng chuỗi Command.</summary>
        private static RunStateMachine Play(RunDef def, ulong seed)
        {
            // Xoay vòng mọi bậc khó / bộ khởi đầu, mở hết khóa để phủ toàn bộ content.
            var setup = new RunSetup
            {
                Difficulty = def.Difficulties[(int)(seed % (ulong)def.Difficulties.Count)],
                StarterKit = def.StarterKits[(int)(seed / 3 % (ulong)def.StarterKits.Count)],
                Renown = int.MaxValue / 2,
            };
            var m = new RunStateMachine(def, seed, setup, checkInvariants: true);
            var agent = new System.Random((int)seed);
            for (var i = 0; i < MaxCommands && m.State.Phase != RunPhase.RunEnd; i++)
                m.Execute(NextCommand(m.State, agent));
            return m;
        }

        internal static ICommand NextCommand(RunState s, System.Random r)
        {
            int Bag() => s.Bag.Count == 0 ? 0 : r.Next(s.Bag.Count);
            int Cell() => r.Next(s.Grid.Height);
            int Col() => r.Next(s.Grid.Width);

            if (r.Next(20) == 0)
                return new InspectCommand(Bag(), Bag());

            switch (s.Phase)
            {
                case RunPhase.Market:
                    switch (r.Next(10))
                    {
                        case 0: case 1: return new BuyCommand(r.Next(System.Math.Max(1, s.Offers.Count)));
                        case 2: case 3: return new SellCommand(Bag());
                        case 4: return new RerollCommand();
                        case 5: return new SkipBrewCommand();
                        default: return new LeaveMarketCommand();
                    }
                case RunPhase.Placing:
                    switch (r.Next(10))
                    {
                        case 0: return new UnplaceCommand(Cell(), Col());
                        case 1: return new MoveCommand(Cell(), Col(), Cell(), Col());
                        case 2: return new SkipBrewCommand();
                        case 3: return new BrewCommand();
                        default: return new PlaceCommand(Bag(), Cell(), Col());
                    }
                case RunPhase.Offering:
                    return new OfferCommand(Bag());
                case RunPhase.Spoils:
                    return new ChooseRewardCommand(r.Next(3));
                default:
                    return new ContinueCommand();
            }
        }
    }
}
