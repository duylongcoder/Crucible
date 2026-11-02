using Alchemy.Core;
using NUnit.Framework;
using static Alchemy.Tests.EditMode.TestContent;

namespace Alchemy.Tests.EditMode
{
    public class DeterminismTests
    {
        [Test]
        public void Pcg32_MatchesReferenceImplementation()
        {
            // pcg32-demo của pcg-random.org với initstate 42, initseq 54.
            var rng = new Pcg32(42, 54);

            var expected = new uint[] { 0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e };
            foreach (var value in expected)
                Assert.That(rng.NextUInt(), Is.EqualTo(value));
        }

        [TestCase(1)]
        [TestCase(7)]
        [TestCase(100)]
        public void NextInt_StaysWithinBound(int bound)
        {
            var rng = new Pcg32(123, 0);
            for (var i = 0; i < 1000; i++)
                Assert.That(rng.NextInt(bound), Is.InRange(0, bound - 1));
        }

        [Test]
        public void RngStreams_DrawingFromOneStream_DoesNotShiftAnother()
        {
            var a = new RngStreams(5);
            var b = new RngStreams(5);
            a[RngStream.Shop].NextUInt();

            Assert.That(a[RngStream.Chance].NextUInt(), Is.EqualTo(b[RngStream.Chance].NextUInt()));
        }

        [Test]
        public void Brew_SameSeedSameInput_SameHash1000Times()
        {
            var expected = RunScenario(2026);
            for (var i = 0; i < 1000; i++)
                Assert.That(RunScenario(2026), Is.EqualTo(expected), $"lần {i}");
        }

        [Test]
        public void Brew_DifferentSeeds_ProduceDifferentHashes()
        {
            Assert.That(RunScenario(1), Is.Not.EqualTo(RunScenario(2)));
        }

        private static ulong RunScenario(ulong seed)
        {
            var rng = new RngStreams(seed);
            var brewer = CreateBrewer();
            var hash = new StateHash();

            var steel = brewer.Brew(MakeGrid(Raw(Iron), Raw(Salt), Raw(Coal)), rng).Product;
            var next = brewer.Brew(MakeGrid(steel, Raw(Sulfur), Raw(Copper)), rng).Product;
            var bronze = brewer.Brew(MakeGrid(Raw(Iron), null, null, Raw(Coal)), rng).Product;

            steel.AddTo(hash);
            next.AddTo(hash);
            bronze.AddTo(hash);
            rng.AddTo(hash);
            return hash.Value;
        }
    }
}
