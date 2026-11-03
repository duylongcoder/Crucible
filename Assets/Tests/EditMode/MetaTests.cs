using System.Linq;
using Alchemy.Core;
using Alchemy.Persistence;
using NUnit.Framework;
using static Alchemy.Tests.EditMode.TestContent;

namespace Alchemy.Tests.EditMode
{
    /// <summary>M5: Danh vọng, mở khóa, bậc khó, bộ khởi đầu, Sổ công thức, seed.</summary>
    public class MetaTests
    {
        private sealed class MemoryStorage : ISaveStorage
        {
            public string Text;
            public string Read() => Text;
            public void Write(string text) => Text = text;
            public void Delete() => Text = null;
        }

        /// <summary>Đi hết run bằng bỏ qua lượt / Continue: thua mọi trận → trục xuất ở Mùa 3.</summary>
        private static void PlayToEnd(RunStateMachine m)
        {
            for (var i = 0; i < 500 && m.State.Phase != RunPhase.RunEnd; i++)
                m.Execute(m.State.Phase == RunPhase.Market || m.State.Phase == RunPhase.Placing
                    ? new SkipBrewCommand()
                    : m.State.Phase == RunPhase.Offering ? new OfferCommand(Index(m.State, p => p.Def.IsBrewed))
                    : m.State.Phase == RunPhase.Spoils ? new ChooseRewardCommand(0) : (ICommand)new ContinueCommand());
            Assert.That(m.State.Phase, Is.EqualTo(RunPhase.RunEnd));
        }

        private static int Index(RunState s, System.Func<Product, bool> match)
        {
            for (var i = 0; i < s.Bag.Count; i++)
                if (match(s.Bag[i]))
                    return i;
            return -1;
        }

        private static void BrewSteel(RunStateMachine m)
        {
            m.Execute(new ContinueCommand());
            m.Execute(new LeaveMarketCommand());
            m.Execute(new PlaceCommand(Index(m.State, p => p.Def == Iron), 0, 0));
            m.Execute(new PlaceCommand(Index(m.State, p => p.Def == Coal), 0, 2));
            Assert.That(m.Execute(new BrewCommand()).Accepted, Is.True);
            Assert.That(m.State.LastBrew.Product.Def, Is.SameAs(Steel));
        }

        [Test]
        public void Unlock_WhenBrewed_UnlocksItemAndRecipe()
        {
            var m = new RunStateMachine(CreateRun(), 1, checkInvariants: true);
            BrewSteel(m);
            PlayToEnd(m);
            var meta = new MetaState();

            MetaProgress.Apply(meta, m.State);

            Assert.That(meta.UnlockedRecipes, Does.Contain("steel"));
            Assert.That(meta.UnlockedItems, Does.Contain("steel"));
        }

        [Test]
        public void Unlock_WhenBought_UnlocksItemOnly()
        {
            var def = CreateRun();
            def.ShopPool = new[] { Copper };
            def.ShopTierWeights = new[] { 0, 100, 0 };
            def.StartGold = 50;
            var m = new RunStateMachine(def, 1, checkInvariants: true);
            m.Execute(new ContinueCommand());
            Assert.That(m.Execute(new BuyCommand(0)).Accepted, Is.True);
            PlayToEnd(m);
            var meta = new MetaState();

            MetaProgress.Apply(meta, m.State);

            Assert.That(meta.UnlockedItems, Does.Contain("copper"));
            Assert.That(meta.UnlockedRecipes, Does.Not.Contain("copper"));
        }

        [Test]
        public void Renown_FromWinsHighestTierAndDifficulty()
        {
            var def = CreateRun(opponentPower: 0);
            def.Difficulties = new[] { new DifficultyDef("x", "x", def.Difficulties[0].Opponents, 150) };
            var m = new RunStateMachine(def, 1, checkInvariants: true);
            BrewSteel(m);
            PlayToEnd(m);

            // Đối thủ Power 0: thắng Mùa 1 bằng Thép (tier 4), các Mùa sau không có gì để dâng → thua 3 → trục xuất.
            Assert.That(m.State.Wins, Is.EqualTo(1));
            Assert.That(m.State.HighestTier, Is.EqualTo(4));
            Assert.That(MetaProgress.RenownFor(m.State), Is.EqualTo((1 * 10 + 4 * 5) * 150 / 100));
        }

        [Test]
        public void Difficulty_WinRun_UnlocksNextOnce()
        {
            var def = CreateRun(opponentPower: 0);
            var hard = new DifficultyDef("hard", "Khó", def.Difficulties[0].Opponents);
            def.Difficulties = new[] { def.Difficulties[0], hard };
            var meta = new MetaState();
            Assert.That(meta.IsDifficultyUnlocked(def, hard), Is.False);

            var m = new RunStateMachine(def, 1, checkInvariants: true);
            m.State.Wins = 3;
            m.State.Reputation = 1;
            m.State.Outcome = RunOutcome.Won;
            m.State.Phase = RunPhase.RunEnd;
            var rewards = MetaProgress.Apply(meta, m.State);

            Assert.That(rewards.DifficultyUnlocked, Is.SameAs(hard));
            Assert.That(meta.IsDifficultyUnlocked(def, hard), Is.True);
            Assert.That(MetaProgress.Apply(meta, m.State).DifficultyUnlocked, Is.Null);
        }

        [Test]
        public void Apply_BeforeRunEnd_Throws()
        {
            var m = new RunStateMachine(CreateRun(), 1);

            Assert.Throws<System.InvalidOperationException>(() => MetaProgress.Apply(new MetaState(), m.State));
        }

        [TestCase(0, false)]
        [TestCase(49, false)]
        [TestCase(50, true)]
        public void Market_LockedItem_OfferedOnlyAtRenown(int renown, bool offered)
        {
            var def = CreateRun();
            var locked = new ItemDef("gold", ItemKind.Mineral, 1, marketPrice: 3, unlockRenown: 50);
            def.Items = def.Items.Append(locked).ToArray();
            def.ShopPool = new[] { locked };
            var m = new RunStateMachine(def, 1, new RunSetup { Renown = renown }, checkInvariants: true);

            m.Execute(new ContinueCommand());

            Assert.That(m.State.Offers.Any(o => o.Item == locked), Is.EqualTo(offered));
        }

        [Test]
        public void StarterKit_LockedAboveRenown_Rejected()
        {
            var def = CreateRun();
            var locked = new StarterKitDef("locked", "Khóa", new[] { Iron }, unlockRenown: 40);
            def.StarterKits = new[] { def.StarterKits[0], locked };

            Assert.Throws<System.ArgumentException>(() => new RunStateMachine(def, 1, new RunSetup { StarterKit = locked, Renown = 39 }));
            Assert.That(new RunStateMachine(def, 1, new RunSetup { StarterKit = locked, Renown = 40 }).State.Bag.Count, Is.EqualTo(1));
        }

        [Test]
        public void Run_UsesOpponentsOfChosenDifficulty()
        {
            var def = CreateRun();
            var hard = new DifficultyDef("hard", "Khó", Enumerable.Range(0, 8)
                .Select(i => new OpponentDef("h" + i, "Khó " + i, BattleKind.War, 99)).ToArray());
            def.Difficulties = new[] { def.Difficulties[0], hard };

            var m = new RunStateMachine(def, 1, new RunSetup { Difficulty = hard });

            Assert.That(m.State.Opponent.Name, Is.EqualTo("Khó 0"));
        }

        [Test]
        public void RecipeBook_Ingredients_OnlyIngredientsInOrder()
        {
            var def = CreateRun();

            Assert.That(RecipeBook.Ingredients(def, Bronze).Select(i => i.Id), Is.EqualTo(new[] { "iron", "coal" }));
            Assert.That(RecipeBook.Ingredients(def, Iron), Is.Empty);
        }

        [Test]
        public void RecipeBook_Entries_MetaOrBrewedThisRun()
        {
            var def = CreateRun();
            var m = new RunStateMachine(def, 1);
            Assert.That(RecipeBook.Entries(def, new MetaState(), m.State), Is.Empty);

            BrewSteel(m);

            Assert.That(RecipeBook.Entries(def, new MetaState(), m.State), Is.EqualTo(new[] { Steel }));
        }

        [TestCase("12345", 12345UL)]
        [TestCase("  42 ", 42UL)]
        public void RunSeed_Digits_ParsedAsNumber(string text, ulong expected)
        {
            Assert.That(RunSeed.Parse(text), Is.EqualTo(expected));
        }

        [Test]
        public void RunSeed_Text_StableHashEmptyIsNull()
        {
            Assert.That(RunSeed.Parse("lo-gia-kim"), Is.EqualTo(RunSeed.Parse("lo-gia-kim")));
            Assert.That(RunSeed.Parse("lo-gia-kim"), Is.Not.EqualTo(RunSeed.Parse("lo-gia-kin")));
            Assert.That(RunSeed.Parse(""), Is.Null);
            Assert.That(RunSeed.Parse(null), Is.Null);
        }

        [Test]
        public void Seed_SameSeed_SameFirstMarketPricesAndForge()
        {
            var def = MvpRun();
            RunStateMachine Play()
            {
                var m = new RunStateMachine(def, RunSeed.Parse("lo-gia-kim").Value);
                m.Execute(new ContinueCommand());
                return m;
            }

            var a = Play();
            var b = Play();

            Assert.That(a.State.Offers.Select(o => o.Item.Id + o.Price), Is.EqualTo(b.State.Offers.Select(o => o.Item.Id + o.Price)));
            Assert.That(a.State.PriceFactors, Is.EqualTo(b.State.PriceFactors));
            Assert.That(a.State.ComputeHash(), Is.EqualTo(b.State.ComputeHash()));
        }

        [Test]
        public void MetaStore_RoundTrip()
        {
            var storage = new MemoryStorage();
            var store = MetaStore.Load(storage);
            var m = new RunStateMachine(CreateRun(), 1);
            BrewSteel(m);
            PlayToEnd(m);
            MetaProgress.Apply(store.State, m.State);
            store.Save();

            var loaded = MetaStore.Load(storage).State;

            Assert.That(loaded.Renown, Is.EqualTo(store.State.Renown));
            Assert.That(loaded.UnlockedRecipes, Is.EqualTo(store.State.UnlockedRecipes));
            Assert.That(loaded.UnlockedItems, Is.EqualTo(store.State.UnlockedItems));
        }

        [Test]
        public void Session_RunEnd_SavesMetaThenDeletesRun()
        {
            var def = MvpRun();
            var runStorage = new MemoryStorage();
            var metaStorage = new MemoryStorage();
            var meta = MetaStore.Load(metaStorage);
            var session = RunSession.New(def, 1, null, runStorage, meta);
            for (var i = 0; i < 300 && session.Machine.State.Phase != RunPhase.RunEnd; i++)
                session.Execute(session.Machine.State.Phase == RunPhase.Market ? new SkipBrewCommand() : (ICommand)new ContinueCommand());

            Assert.That(runStorage.Text, Is.Null);
            Assert.That(metaStorage.Text, Is.Not.Null);
            Assert.That(session.Rewards, Is.Not.Null);
            Assert.That(MetaStore.Load(metaStorage).State.UnlockedItems, Does.Contain("iron_ore"), "bộ khởi đầu = vật phẩm đã nhận");
        }

        [Test]
        public void Session_Reload_KeepsDifficultyKitAndRenown()
        {
            var def = MvpRun();
            var storage = new MemoryStorage();
            var setup = new RunSetup { Difficulty = def.Difficulties[2], StarterKit = def.StarterKits[1], Renown = 500 };
            var session = RunSession.New(def, 9, setup, storage);
            session.Execute(new ContinueCommand());

            var loaded = RunSession.Load(def, storage).Machine.State;

            Assert.That(loaded.Difficulty, Is.SameAs(def.Difficulties[2]));
            Assert.That(loaded.StarterKit, Is.SameAs(def.StarterKits[1]));
            Assert.That(loaded.Renown, Is.EqualTo(500));
            Assert.That(loaded.ComputeHash(), Is.EqualTo(session.Machine.State.ComputeHash()));
        }
    }
}
