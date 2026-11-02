using System.Linq;
using Alchemy.Core;
using NUnit.Framework;
using static Alchemy.Tests.EditMode.TestContent;

namespace Alchemy.Tests.EditMode
{
    public class BrewLogTests
    {
        [Test]
        public void BrewLog_PlaysEveryTickFromFirstToLastIngredient_ThenResult()
        {
            var events = Brew(1, null, Raw(Iron), null, Raw(Coal), null, null).Log.Events;

            Assert.That(events.Select(e => (e.Kind, e.Tick, e.ItemId)), Is.EqualTo(new[]
            {
                (BrewEventKind.Feed, 1, "iron"),
                (BrewEventKind.Wait, 2, (string)null),
                (BrewEventKind.Feed, 3, "coal"),
                (BrewEventKind.Result, 3, "steel"),
            }));
        }

        [Test]
        public void BrewLog_WhenFailed_MatchesSuccessUntilResult()
        {
            // Cùng bố cục lưới; chỉ khác Steel có ma thuật hay không.
            var success = Brew(1, Magic(Steel), null, Raw(Copper)).Log.Events;
            var failed = Brew(1, Raw(Steel), null, Raw(Copper)).Log.Events;

            Assert.That(failed.Count, Is.EqualTo(success.Count));
            for (var i = 0; i < success.Count - 1; i++)
            {
                Assert.That(failed[i].Kind, Is.EqualTo(success[i].Kind));
                Assert.That(failed[i].Tick, Is.EqualTo(success[i].Tick));
                Assert.That(failed[i].ItemId, Is.EqualTo(success[i].ItemId));
            }

            Assert.That(success.Last().ItemId, Is.EqualTo("arcanite"));
            Assert.That(failed.Last().ItemId, Is.EqualTo("scrap"));
        }

        [Test]
        public void BrewLog_FullGrid_AtMostNineTicksAndOneResult()
        {
            var cells = Enumerable.Range(0, 9).Select(_ => Raw(Iron)).ToArray();

            var events = Brew(1, cells).Log.Events;

            Assert.That(events.Count(e => e.Kind != BrewEventKind.Result), Is.EqualTo(9));
            Assert.That(events.Count(e => e.Kind == BrewEventKind.Result), Is.EqualTo(1));
            Assert.That(events.Last().Kind, Is.EqualTo(BrewEventKind.Result));
        }
    }
}
