using Alchemy.Core;
using NUnit.Framework;
using static Alchemy.Tests.EditMode.TestContent;

namespace Alchemy.Tests.EditMode
{
    public class GridTests
    {
        [Test]
        public void ToFeedSequence_ReadsRowMajor_EmptyCellsAreWaits()
        {
            Product a = Raw(Iron), b = Raw(Coal), c = Raw(Copper), d = Raw(Flux), e = Raw(Salt);
            var grid = MakeGrid(
                null, a, null,
                b, null, null,
                c, d, e);

            Assert.That(grid.ToFeedSequence(), Is.EqualTo(new[] { null, a, null, b, null, null, c, d, e }));
        }

        [Test]
        public void Place_WhenCellOccupied_Rejects()
        {
            var grid = MakeGrid(Raw(Iron));

            Assert.That(grid.Place(0, 0, Raw(Coal)), Is.False);
            Assert.That(grid[0, 0].Def, Is.SameAs(Iron));
        }

        [Test]
        public void Place_WhenScrap_Rejects()
        {
            var grid = MakeGrid();

            Assert.That(grid.Place(0, 0, Raw(Scrap)), Is.False);
            Assert.That(grid[0, 0], Is.Null);
        }

        [Test]
        public void Remove_ReturnsItemAndEmptiesCell()
        {
            var iron = Raw(Iron);
            var grid = MakeGrid(iron);

            Assert.That(grid.Remove(0, 0), Is.SameAs(iron));
            Assert.That(grid[0, 0], Is.Null);
        }

        [TestCase(-1, 0)]
        [TestCase(0, 3)]
        [TestCase(3, 0)]
        public void Place_WhenOutsideGrid_Throws(int row, int col)
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => MakeGrid().Place(row, col, Raw(Iron)));
        }
    }
}
