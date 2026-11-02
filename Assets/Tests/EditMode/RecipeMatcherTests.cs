using Alchemy.Core;
using NUnit.Framework;
using static Alchemy.Tests.EditMode.TestContent;

namespace Alchemy.Tests.EditMode
{
    public class RecipeMatcherTests
    {
        [Test]
        public void Match_WhenExactPattern_ProducesProduct()
        {
            var result = Brew(1, Raw(Iron), null, Raw(Coal));

            Assert.That(result.Product.Def, Is.SameAs(Steel));
            Assert.That(result.Product.Quality, Is.EqualTo(6));
        }

        [Test]
        public void Match_SameIngredientsDifferentGap_ProducesDifferentProducts()
        {
            var steel = Brew(1, Raw(Iron), null, Raw(Coal)).Product;
            var bronze = Brew(1, Raw(Iron), null, null, Raw(Coal)).Product;

            Assert.That(steel.Def, Is.SameAs(Steel));
            Assert.That(bronze.Def, Is.SameAs(Bronze));
        }

        [TestCase(1)] // Iron sát Coal
        [TestCase(4)] // một tick dư so với Bronze
        public void Match_WhenOffByOneTick_ProducesScrap(int coalIndex)
        {
            var cells = new Product[9];
            cells[0] = Raw(Iron);
            cells[coalIndex] = Raw(Coal);

            Assert.That(Brew(1, cells).Product.Def, Is.SameAs(Scrap));
        }

        [Test]
        public void Match_WithLeadingEmptyCells_StillMatches()
        {
            var result = Brew(1, null, null, null, null, Raw(Iron), null, Raw(Coal));

            Assert.That(result.Product.Def, Is.SameAs(Steel));
        }

        [Test]
        public void Match_WhenItemAfterPattern_ProducesScrap()
        {
            var result = Brew(1, Raw(Iron), null, Raw(Coal), null, Raw(Copper));

            Assert.That(result.Product.Def, Is.SameAs(Scrap));
        }

        [Test]
        public void Match_WithAdditiveInGap_SameProductAndQuality()
        {
            var plain = Brew(7, Raw(Iron), null, Raw(Coal)).Product;
            var withAdditive = Brew(7, Raw(Iron), Raw(Flux), Raw(Coal)).Product;

            Assert.That(withAdditive.Def, Is.SameAs(plain.Def));
            Assert.That(withAdditive.Essence, Is.EqualTo(plain.Essence));
            Assert.That(withAdditive.Potency, Is.EqualTo(plain.Potency));
            Assert.That(withAdditive.Quality, Is.EqualTo(plain.Quality));
        }

        [Test]
        public void Match_WhenAdditiveNotAllowed_ProducesScrap()
        {
            Assert.That(Brew(1, Raw(Iron), Raw(Sulfur), Raw(Coal)).Product.Def, Is.SameAs(Scrap));
        }

        [Test]
        public void Match_WhenAdditiveOutsideGap_ProducesScrap()
        {
            Assert.That(Brew(1, Raw(Iron), null, Raw(Coal), Raw(Flux)).Product.Def, Is.SameAs(Scrap));
        }

        [Test]
        public void Match_WhenMineralInGap_ProducesScrap()
        {
            Assert.That(Brew(1, Raw(Iron), Raw(Copper), Raw(Coal)).Product.Def, Is.SameAs(Scrap));
        }

        [Test]
        public void Match_WhenRequiresMagicAndIngredientNotMagic_ProducesScrap()
        {
            Assert.That(Brew(1, Raw(Steel), null, Raw(Copper)).Product.Def, Is.SameAs(Scrap));
        }

        [Test]
        public void Match_WhenRequiresMagicAndIngredientMagic_Matches()
        {
            Assert.That(Brew(1, Magic(Steel), null, Raw(Copper)).Product.Def, Is.SameAs(Arcanite));
        }

        [Test]
        public void Match_WhenScrapInFeed_NeverMatchesEvenIfRecipeNamesIt()
        {
            var badRecipe = new Recipe(new[] { new RecipeSlot("scrap"), RecipeSlot.Gap, new RecipeSlot("coal") }, 1);

            Assert.That(RecipeMatcher.Matches(badRecipe, new[] { Raw(Scrap), null, Raw(Coal) }), Is.False);
        }

        [Test]
        public void Match_WhenTwoRecipesMatch_Throws()
        {
            var duplicate = new ItemDef("steel2", ItemKind.Mineral, 4, Steel.Recipe);
            var matcher = new RecipeMatcher(new[] { Steel, duplicate });

            Assert.Throws<System.InvalidOperationException>(() => matcher.Match(new[] { Raw(Iron), null, Raw(Coal) }));
        }

        [Test]
        public void Brew_WhenGridEmpty_ProducesNothing()
        {
            var result = Brew(1);

            Assert.That(result.Product, Is.Null);
            Assert.That(result.Log.Events, Is.Empty);
        }

        [Test]
        public void Brew_WhenScrap_HasNoStats()
        {
            Assert.That(Brew(1, Raw(Iron), Raw(Coal)).Product.Stats, Is.Empty);
        }
    }
}
