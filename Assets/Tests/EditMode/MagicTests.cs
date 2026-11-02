using Alchemy.Core;
using NUnit.Framework;
using static Alchemy.Tests.EditMode.TestContent;

namespace Alchemy.Tests.EditMode
{
    public class MagicTests
    {
        [Test]
        public void Magic_ChanceWithoutMagicIngredientOrAdditive_IsZero()
        {
            Assert.That(ProductFactory.MagicChance(new[] { Raw(Iron), null, Raw(Coal) }, Rules), Is.EqualTo(0));
        }

        [Test]
        public void Magic_ChanceSumsInheritanceAndAdditives()
        {
            // Steel tier 4 đã kích hoạt: 4 × 5 = 20; Sulfur: 30.
            var feed = new[] { Magic(Steel), Raw(Sulfur), Raw(Copper) };

            Assert.That(ProductFactory.MagicChance(feed, Rules), Is.EqualTo(50));
        }

        [Test]
        public void Magic_ChanceSumsInheritanceAndAdditives_CappedAt90()
        {
            var feed = new[] { Magic(Arcanite), Raw(Sulfur), Raw(Sulfur), Raw(Sulfur), Raw(Copper) };

            Assert.That(ProductFactory.MagicChance(feed, Rules), Is.EqualTo(90));
        }

        [Test]
        public void Magic_WhenChanceZero_NeverActivates()
        {
            for (ulong seed = 0; seed < 300; seed++)
                Assert.That(Brew(seed, Raw(Iron), null, Raw(Coal)).Product.IsMagic, Is.False, $"seed {seed}");
        }

        [Test]
        public void Magic_WhenChancePositive_ActivatesOnlyForSomeSeeds()
        {
            var magicCount = 0;
            for (ulong seed = 0; seed < 300; seed++)
                if (Brew(seed, Raw(Iron), Raw(Salt), Raw(Coal)).Product.IsMagic)
                    magicCount++;

            // 20%: kỳ vọng ~60/300.
            Assert.That(magicCount, Is.InRange(20, 120));
        }

        [Test]
        public void Magic_AdditiveChangesOnlyMagicLine_OtherRollsUnchanged()
        {
            for (ulong seed = 0; seed < 100; seed++)
            {
                var plain = Brew(seed, Raw(Iron), null, Raw(Coal)).Product;
                var withAdditive = Brew(seed, Raw(Iron), Raw(Salt), Raw(Coal)).Product;

                Assert.That(withAdditive.HasStat("sharpness"), Is.EqualTo(plain.HasStat("sharpness")), $"seed {seed}");
            }
        }
    }
}
