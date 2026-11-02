using Alchemy.Core;
using NUnit.Framework;

namespace Alchemy.Tests.EditMode
{
    public class ScoreCalculatorTests
    {
        private static readonly double[] None = { };

        [Test]
        public void Quality_StandardExample_Is108()
        {
            // 18 Essence, +3 Potency, ×1.5.
            var quality = ScoreCalculator.Quality(new[] { 10.0, 8.0 }, new[] { 1.0, 2.0 }, new[] { 1.5 });

            Assert.That(quality, Is.EqualTo(108));
        }

        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NaN)]
        [TestCase(1e300)]
        public void Quality_WhenExtremeInput_StaysFiniteAndNonNegative(double extreme)
        {
            var quality = ScoreCalculator.Quality(new[] { extreme }, new[] { extreme }, new[] { 5.0, 5.0 });

            Assert.That(double.IsNaN(quality) || double.IsInfinity(quality), Is.False);
            Assert.That(quality, Is.InRange(0, ScoreCalculator.MaxValue));
        }

        [Test]
        public void Quality_WhenEssenceNegative_IsZero()
        {
            Assert.That(ScoreCalculator.Quality(new[] { -5.0 }, None, None), Is.EqualTo(0));
        }

        [TestCase(-10.0, 1.0)]
        [TestCase(-0.5, 0.1)]
        public void Potency_WhenReducedBelowOne_ClampedToOne(double add, double mul)
        {
            Assert.That(ScoreCalculator.Potency(new[] { add }, new[] { mul }), Is.EqualTo(1));
        }
    }
}
