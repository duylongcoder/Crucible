using System.Collections.Generic;

namespace Alchemy.Core
{
    /// <summary>Xử lý trọn một lượt nung ngay lập tức; Presentation phát lại <see cref="BrewLog"/> theo nhịp.</summary>
    internal sealed class Brewer
    {
        private readonly BrewRules _rules;
        private readonly RecipeMatcher _matcher;
        private readonly ItemDef _scrap;

        public Brewer(BrewRules rules, IEnumerable<ItemDef> items, ItemDef scrap)
        {
            _rules = rules;
            _matcher = new RecipeMatcher(items);
            _scrap = scrap;
        }

        public BrewResult Brew(Grid grid, RngStreams rng)
        {
            var feed = grid.ToFeedSequence();
            if (feed.Length > _rules.CellCount)
                throw new System.InvalidOperationException($"I1: lưới {feed.Length} ô vượt {_rules.CellCount} tick.");

            var first = 0;
            while (first < feed.Length && feed[first] == null)
                first++;
            if (first == feed.Length)
                return new BrewResult(null, new BrewLog(System.Array.Empty<BrewEvent>()));

            var last = feed.Length - 1;
            while (feed[last] == null)
                last--;

            // Sự kiện tick chỉ phụ thuộc lưới, không phụ thuộc kết quả khớp: lượt khớp và lượt hỏng giống nhau tới đây (I15).
            var events = new List<BrewEvent>(last - first + 2);
            for (var t = first; t <= last; t++)
                events.Add(feed[t] == null
                    ? new BrewEvent(BrewEventKind.Wait, t, null)
                    : new BrewEvent(BrewEventKind.Feed, t, feed[t].Def.Id));

            var matched = _matcher.Match(feed);
            var product = matched == null
                ? new Product(_scrap)
                : ProductFactory.Create(matched, feed, _rules, rng[RngStream.Chance]);

            events.Add(new BrewEvent(BrewEventKind.Result, last, product.Def.Id));
            return new BrewResult(product, new BrewLog(events));
        }
    }
}
