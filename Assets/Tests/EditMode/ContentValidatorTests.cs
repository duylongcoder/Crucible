using System.Collections.Generic;
using Alchemy.Content;
using Alchemy.Core;
using NUnit.Framework;
using UnityEngine;

namespace Alchemy.Tests.EditMode
{
    /// <summary>Validator (CLAUDE.md mục 7): content MVP hợp lệ; mỗi loại lỗi content làm validate fail.</summary>
    public class ContentValidatorTests
    {
        private readonly List<Object> _created = new List<Object>();
        private RunDefAsset _run;
        private ItemDefAsset _a, _b, _add, _x, _top;

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _created)
                Object.DestroyImmediate(o);
            _created.Clear();
        }

        private T New<T>() where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            _created.Add(o);
            return o;
        }

        private ItemDefAsset Item(string id, ItemKind kind, int tier, params SlotData[] pattern)
        {
            var i = New<ItemDefAsset>();
            i.id = id;
            i.kind = kind;
            i.tier = tier;
            i.recipe.pattern.AddRange(pattern);
            return i;
        }

        private static SlotData S(ItemDefAsset item, bool magic = false) => new SlotData { item = item, requiresMagic = magic };

        private static SlotData Gap(bool magic = false) => new SlotData { requiresMagic = magic };

        /// <summary>Run nhỏ hợp lệ: a · gap · b → x (tier 2); x(ma thuật) · gap · a → top (tier 10).</summary>
        [SetUp]
        public void SetUp()
        {
            var quality = New<StatDefAsset>();
            quality.id = StatIds.Quality;
            var magic = New<StatDefAsset>();
            magic.id = StatIds.Magic;

            _a = Item("a", ItemKind.Mineral, 1);
            _b = Item("b", ItemKind.Mineral, 1);
            _add = Item("add", ItemKind.Additive, 1);
            _add.magicChance = 10;
            _x = Item("x", ItemKind.Mineral, 2, S(_a), Gap(), S(_b));
            _x.recipe.allowedAdditives.Add(_add);
            _top = Item("top", ItemKind.Mineral, 10, S(_x, true), Gap(), S(_a));
            var scrap = Item("scrap", ItemKind.Scrap, 0);

            var opponent = New<OpponentDefAsset>();
            opponent.id = "o";
            var equipment = New<EquipmentDefAsset>();
            equipment.id = "e";

            _run = New<RunDefAsset>();
            _run.id = "test";
            _run.seasonsPerRun = 1;
            _run.stats = new List<StatDefAsset> { quality, magic };
            _run.items = new List<ItemDefAsset> { _a, _b, _add, _x, _top };
            _run.scrap = scrap;
            _run.shopPool = new List<ItemDefAsset> { _a, _b, _add };
            var kit = New<StarterKitDefAsset>();
            kit.id = "kit";
            kit.items.Add(_a);
            var difficulty = New<DifficultyDefAsset>();
            difficulty.id = "normal";
            difficulty.opponents.Add(opponent);
            _run.starterKits = new List<StarterKitDefAsset> { kit };
            _run.difficulties = new List<DifficultyDefAsset> { difficulty };
            _run.equipment = new List<EquipmentDefAsset> { equipment };
        }

        private void AssertFails(string fragment)
        {
            var errors = ContentValidator.Validate(_run);
            Assert.That(errors, Has.Some.Contains(fragment), string.Join("\n", errors));
        }

        [Test]
        public void Validate_MvpContent_HasNoErrors()
        {
            var errors = ContentValidator.Validate(TestContent.MvpRunAsset());

            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [Test]
        public void Validate_MvpContent_ConvertsAndStartsRun()
        {
            var m = new RunStateMachine(TestContent.MvpRun(), 1, checkInvariants: true);

            Assert.That(m.State.Phase, Is.EqualTo(RunPhase.Decree));
        }

        [Test]
        public void Validate_SmallValidRun_HasNoErrors()
        {
            Assert.That(ContentValidator.Validate(_run), Is.Empty);
        }

        [Test]
        public void Validate_TwoRecipesMatchSameSequence_Fails()
        {
            // a · add · b khớp cả x (phụ gia ở Gap) lẫn twin (add là nguyên liệu).
            var twin = Item("twin", ItemKind.Mineral, 2, S(_a), S(_add), S(_b));
            _run.items.Add(twin);

            AssertFails("I2");
        }

        [Test]
        public void Validate_SameIngredientsDifferentGaps_Passes()
        {
            _run.items.Add(Item("wide", ItemKind.Mineral, 2, S(_a), Gap(), Gap(), S(_b)));

            Assert.That(ContentValidator.Validate(_run), Is.Empty);
        }

        [Test]
        public void Validate_PatternStartsWithGap_Fails()
        {
            _x.recipe.pattern.Insert(0, Gap());

            AssertFails("bắt đầu và kết thúc");
        }

        [Test]
        public void Validate_PatternLongerThanGrid_Fails()
        {
            for (var i = 0; i < 8; i++)
                _x.recipe.pattern.Insert(1, Gap());

            AssertFails("Pattern dài");
        }

        [Test]
        public void Validate_RequiresMagicOnGap_Fails()
        {
            _x.recipe.pattern[1] = Gap(magic: true);

            AssertFails("RequiresMagic trên Gap");
        }

        [Test]
        public void Validate_AdditiveWithEffect_Fails()
        {
            _add.effects.Add(new EffectData { trigger = EffectTrigger.OnRecipeMatch, action = EffectAction.AddEssence, value = 1 });

            AssertFails("phụ gia chỉ được dùng AddMagicChance");
        }

        [Test]
        public void Validate_RecipeItemInShopPool_Fails()
        {
            _run.shopPool.Add(_x);

            AssertFails("I24");
        }

        [Test]
        public void Validate_ShopItemAboveShopMaxTier_Fails()
        {
            var rare = Item("rare", ItemKind.Mineral, 4);
            _run.items.Add(rare);
            _run.shopPool.Add(rare);

            AssertFails("I24");
        }

        [Test]
        public void Validate_Tier10WithoutMagicSource_Fails()
        {
            // Không còn phụ gia ma thuật nào tới được x → top (RequiresMagic) không nung được.
            _x.recipe.allowedAdditives.Clear();

            AssertFails("'top'");
        }

        [Test]
        public void Validate_NoTier10Mineral_Fails()
        {
            _run.items.Remove(_top);

            AssertFails("Thiếu khoáng tier 10");
        }

        [Test]
        public void Validate_MulPotencyAboveFive_Fails()
        {
            var cat = Item("cat", ItemKind.Catalyst, 1);
            cat.effects.Add(new EffectData { trigger = EffectTrigger.OnRecipeMatch, action = EffectAction.MulPotency, value = 6 });
            _run.items.Add(cat);

            AssertFails("MulPotency");
        }

        [Test]
        public void Validate_ProductActionOutsideRecipeMatch_Fails()
        {
            var cat = Item("cat", ItemKind.Catalyst, 1);
            cat.effects.Add(new EffectData { trigger = EffectTrigger.OnShopEnter, action = EffectAction.AddEssence, value = 1 });
            _run.items.Add(cat);

            AssertFails("chỉ dùng với OnRecipeMatch");
        }

        [Test]
        public void Validate_DuplicateItemId_Fails()
        {
            _b.id = "a";

            AssertFails("trùng id 'a'");
        }

        [Test]
        public void Validate_WrongOpponentCount_Fails()
        {
            _run.seasonsPerRun = 2;

            AssertFails("đối thủ");
        }

        [Test]
        public void Validate_NullListEntry_FailsWithoutThrowing()
        {
            _run.items.Add(null);

            AssertFails("items có phần tử trống");
        }
    }
}
