using System.Collections.Generic;
using System.Linq;
using Alchemy.Core;
using NUnit.Framework;
using static Alchemy.Tests.EditMode.TestContent;

namespace Alchemy.Tests.EditMode
{
    /// <summary>Xúc tác: EffectRunner, giới hạn 5 Xúc tác trong Túi, hiệu ứng vàng / tạo nguyên liệu.</summary>
    public class EffectTests
    {
        private static ItemDef Catalyst(string id, params EffectDef[] effects) =>
            new ItemDef(id, ItemKind.Catalyst, 1, marketPrice: 5, effects: effects);

        private static EffectDef OnMatch(EffectAction action, double value, params EffectCondition[] conditions) =>
            new EffectDef(EffectTrigger.OnRecipeMatch, action, value, conditions: conditions);

        // Thép: iron · gap · coal, BaseEssence 6.
        private static BrewResult BrewSteel(ulong seed, params ItemDef[] catalysts) =>
            CreateBrewer().Brew(MakeGrid(Raw(Iron), null, Raw(Coal)), new RngStreams(seed), Products(catalysts));

        private static List<Product> Products(IEnumerable<ItemDef> defs)
        {
            var list = new List<Product>();
            foreach (var d in defs)
                list.Add(new Product(d));
            return list;
        }

        [Test]
        public void Catalyst_EssenceAddMulPotency_QualityIs108RegardlessOfOrder()
        {
            var mul = Catalyst("mul", OnMatch(EffectAction.MulPotency, 1.5));
            var add = Catalyst("add", OnMatch(EffectAction.AddPotency, 3));
            var essence = Catalyst("essence", OnMatch(EffectAction.AddEssence, 12));

            Assert.That(BrewSteel(1, mul, add, essence).Product.Quality, Is.EqualTo(108));
            Assert.That(BrewSteel(1, essence, add, mul).Product.Quality, Is.EqualTo(108));
        }

        [Test]
        public void Catalyst_RecipeIsOtherProduct_DoesNotApply()
        {
            var onBronze = Catalyst("c", OnMatch(EffectAction.AddEssence, 10,
                new EffectCondition(EffectConditionKind.RecipeIs, "bronze")));
            var onIron = Catalyst("d", OnMatch(EffectAction.AddEssence, 10,
                new EffectCondition(EffectConditionKind.IngredientIs, "iron")));

            Assert.That(BrewSteel(1, onBronze).Product.Quality, Is.EqualTo(6));
            Assert.That(BrewSteel(1, onIron).Product.Quality, Is.EqualTo(16));
        }

        [Test]
        public void Catalyst_AddStat_AddsToExistingLineOrNewLine()
        {
            var c = Catalyst("c",
                new EffectDef(EffectTrigger.OnRecipeMatch, EffectAction.AddStat, 3, "thorns"),
                new EffectDef(EffectTrigger.OnRecipeMatch, EffectAction.AddStat, 2, "thorns"));

            var product = BrewSteel(1, c).Product;

            var lines = product.Stats.Count(s => s.StatId == "thorns");
            Assert.That(lines, Is.EqualTo(1));
            Assert.That(product.Stats.First(s => s.StatId == "thorns").Value, Is.EqualTo(5));
        }

        [Test]
        public void Catalyst_WithAdditiveInGap_SameQuality()
        {
            var lucky = Catalyst("lucky", OnMatch(EffectAction.MulPotency, 2, new EffectCondition(EffectConditionKind.Chance, amount: 50)));
            for (ulong seed = 0; seed < 50; seed++)
            {
                var plain = CreateBrewer().Brew(MakeGrid(Raw(Iron), null, Raw(Coal)), new RngStreams(seed), Products(new[] { lucky }));
                var withFlux = CreateBrewer().Brew(MakeGrid(Raw(Iron), Raw(Flux), Raw(Coal)), new RngStreams(seed), Products(new[] { lucky }));

                Assert.That(withFlux.Product.Def, Is.SameAs(plain.Product.Def));
                Assert.That(withFlux.Product.Quality, Is.EqualTo(plain.Product.Quality), $"seed {seed}");
            }
        }

        [Test]
        public void Catalyst_OnBrewFail_GainsGoldOnlyOnScrap()
        {
            var sieve = Catalyst("sieve", new EffectDef(EffectTrigger.OnBrewFail, EffectAction.GainGold, 2));

            var fail = CreateBrewer().Brew(MakeGrid(Raw(Iron), Raw(Coal)), new RngStreams(1), Products(new[] { sieve }));

            Assert.That(fail.Product.Def, Is.SameAs(Scrap));
            Assert.That(fail.Effects.Gold, Is.EqualTo(2));
            Assert.That(BrewSteel(1, sieve).Effects.Gold, Is.EqualTo(0));
        }

        // ---- qua RunStateMachine ----

        private static RunStateMachine RunWith(IReadOnlyList<ItemDef> starter, IReadOnlyList<ItemDef> shop = null, int bag = 10)
        {
            var def = CreateRun();
            var items = new List<ItemDef>(def.Items);
            foreach (var i in starter)
                if (!items.Contains(i))
                    items.Add(i);
            if (shop != null)
                foreach (var i in shop)
                    if (!items.Contains(i))
                        items.Add(i);
            def.Items = items;
            def.StarterKits = new[] { new StarterKitDef("k", "k", starter) };
            def.BagCapacity = bag;
            if (shop != null)
            {
                def.ShopPool = shop;
                def.ShopTierWeights = new[] { 100, 0, 0 };
            }

            return new RunStateMachine(def, 3, checkInvariants: true);
        }

        private static int IndexOf(RunState s, ItemDef def)
        {
            for (var i = 0; i < s.Bag.Count; i++)
                if (s.Bag[i].Def == def)
                    return i;
            return -1;
        }

        private static void ToPlacing(RunStateMachine m)
        {
            Assert.That(m.Execute(new ContinueCommand()).Accepted, Is.True);
            Assert.That(m.Execute(new LeaveMarketCommand()).Accepted, Is.True);
        }

        [Test]
        public void Brew_WithGoldCatalyst_AddsGoldAndReportsIt()
        {
            var sieve = Catalyst("sieve", new EffectDef(EffectTrigger.OnBrewFail, EffectAction.GainGold, 2));
            var m = RunWith(new[] { Iron, Coal, sieve });
            ToPlacing(m);
            m.Execute(new PlaceCommand(IndexOf(m.State, Iron), 0, 0));
            m.Execute(new PlaceCommand(IndexOf(m.State, Coal), 0, 1));

            Assert.That(m.Execute(new BrewCommand()).Accepted, Is.True);

            Assert.That(m.State.Gold, Is.EqualTo(2));
            Assert.That(m.State.LastBrew.GoldGained, Is.EqualTo(2));
        }

        [Test]
        public void CreateIngredient_WhenBagFull_IsBlocked()
        {
            var maker = Catalyst("maker", new EffectDef(EffectTrigger.OnBrewEnd, EffectAction.CreateIngredient, targetId: "coal"));
            // Túi 3 chỗ: nung iron + coal → còn maker + thành phẩm, tạo được 1 coal rồi đầy.
            var m = RunWith(new[] { Iron, Coal, maker }, bag: 3);
            ToPlacing(m);
            m.Execute(new PlaceCommand(IndexOf(m.State, Iron), 0, 0));
            m.Execute(new PlaceCommand(IndexOf(m.State, Coal), 0, 2));
            m.Execute(new BrewCommand());

            Assert.That(m.State.Bag.Count, Is.EqualTo(3));
            Assert.That(m.State.LastBrew.Created.Count, Is.EqualTo(1));

            // Lượt sau Túi đầy sẵn: không tạo thêm.
            m.Execute(new ContinueCommand());
            m.Execute(new LeaveMarketCommand());
            m.Execute(new PlaceCommand(IndexOf(m.State, Coal), 0, 0));
            m.Execute(new BrewCommand());
            Assert.That(m.State.Bag.Count, Is.EqualTo(3));
            Assert.That(m.State.LastBrew.Created, Is.Empty);
        }

        [Test]
        public void ShopEnter_CreateIngredient_AddsToBag()
        {
            var panner = Catalyst("panner", new EffectDef(EffectTrigger.OnShopEnter, EffectAction.CreateIngredient, targetId: "coal"));
            var m = RunWith(new[] { panner });

            m.Execute(new ContinueCommand());

            Assert.That(m.State.Phase, Is.EqualTo(RunPhase.Market));
            Assert.That(IndexOf(m.State, Coal), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void Shop_Buy_WhenCatalystSlotsFull_Rejects()
        {
            var shopCatalyst = Catalyst("shop_cat");
            var held = new List<ItemDef>();
            for (var i = 0; i < 5; i++)
                held.Add(Catalyst("held" + i));
            var m = RunWith(held, new[] { shopCatalyst });
            m.State.Gold = 100;
            m.Execute(new ContinueCommand());

            var result = m.Execute(new BuyCommand(0));

            Assert.That(result.Accepted, Is.False);
            Assert.That(m.State.CatalystCount, Is.EqualTo(5));
        }

        [Test]
        public void Place_Catalyst_Rejects()
        {
            var c = Catalyst("c");
            var m = RunWith(new[] { c });
            ToPlacing(m);

            Assert.That(m.Execute(new PlaceCommand(0, 0, 0)).Accepted, Is.False);
        }
    }
}
