using System.Collections.Generic;
using System.Globalization;
using Alchemy.Core;

namespace Alchemy.Persistence
{
    /// <summary>
    /// Chụp / khôi phục toàn bộ <see cref="RunState"/>. Chỉ chụp ở đầu Decree / Market, lúc chưa có LastBrew,
    /// LastBattle cần phát lại; các phase sau đó được dựng lại bằng replay actions.
    /// </summary>
    internal static class Snapshot
    {
        public static SnapshotData Capture(RunState s)
        {
            var d = new SnapshotData
            {
                season = s.Season,
                phase = (int)s.Phase,
                brewsUsed = s.BrewsUsed,
                reputation = s.Reputation,
                gold = s.Gold,
                wins = s.Wins,
                outcome = (int)s.Outcome,
                bagCapacity = s.BagCapacity,
                rerolls = s.RerollsThisTurn,
                modifier = s.Modifier?.Id ?? "",
                nextModifier = s.NextModifier?.Id ?? "",
            };
            foreach (var p in s.Bag)
                d.bag.Add(Capture(p));
            foreach (var cell in s.Grid.ToFeedSequence())
                d.grid.Add(cell == null ? new ProductData { id = "" } : Capture(cell));
            foreach (var o in s.Offers)
                d.offers.Add(new OfferData { id = o.Item.Id, price = o.Price, sold = o.Sold });
            foreach (var r in s.RewardChoices)
                d.rewards.Add(r.Id);
            if (s.PriceFactors != null)
                d.priceFactors.AddRange(s.PriceFactors);
            foreach (RngStream stream in System.Enum.GetValues(typeof(RngStream)))
                d.rng.Add(s.Rng[stream].State.ToString(CultureInfo.InvariantCulture));
            return d;
        }

        /// <summary>Ném <see cref="System.FormatException"/> khi data không khớp RunDef.</summary>
        public static RunState Restore(RunDef def, ulong seed, SnapshotData d)
        {
            var s = new RunState(def, seed)
            {
                Season = d.season,
                Phase = (RunPhase)d.phase,
                BrewsUsed = d.brewsUsed,
                Reputation = d.reputation,
                Gold = d.gold,
                Wins = d.wins,
                Outcome = (RunOutcome)d.outcome,
                BagCapacity = d.bagCapacity,
                RerollsThisTurn = d.rerolls,
                Modifier = Find(def.Modifiers, d.modifier, m => m.Id),
                NextModifier = Find(def.Modifiers, d.nextModifier, m => m.Id),
                Grid = new Grid(def.Brew.GridWidth, def.Brew.GridHeight),
                PriceFactors = d.priceFactors.Count == 0 ? null : d.priceFactors.ToArray(),
            };

            foreach (var p in d.bag)
                s.BagItems.Add(Restore(def, p));
            if (d.grid.Count != def.Brew.CellCount)
                throw new System.FormatException("Lưới trong save sai kích thước.");
            for (var i = 0; i < d.grid.Count; i++)
                if (d.grid[i].id != "")
                    s.Grid.Place(i / s.Grid.Width, i % s.Grid.Width, Restore(def, d.grid[i]));
            foreach (var o in d.offers)
                s.OfferList.Add(new ShopOffer(Item(def, o.id), o.price) { Sold = o.sold });
            foreach (var id in d.rewards)
                s.RewardList.Add(Find(def.Rewards, id, r => r.Id) ?? throw new System.FormatException($"Không có chiến lợi phẩm '{id}'."));

            var streams = (RngStream[])System.Enum.GetValues(typeof(RngStream));
            if (d.rng.Count != streams.Length)
                throw new System.FormatException("Số RNG stream trong save sai.");
            for (var i = 0; i < streams.Length; i++)
                s.Rng[streams[i]].State = ulong.Parse(d.rng[i], CultureInfo.InvariantCulture);
            return s;
        }

        private static ProductData Capture(Product p)
        {
            var d = new ProductData { id = p.Def.Id, essence = Bits(p.Essence), potency = Bits(p.Potency) };
            for (var i = 0; i < p.Stats.Count; i++)
                d.stats.Add(new StatData { id = p.Stats[i].StatId, value = Bits(p.Stats[i].Value), revealed = p.IsRevealed(i) });
            return d;
        }

        private static Product Restore(RunDef def, ProductData d)
        {
            var stats = new List<StatLine>();
            foreach (var s in d.stats)
                stats.Add(new StatLine(s.id, Double(s.value)));
            var product = new Product(Item(def, d.id), Double(d.essence), Double(d.potency), stats);
            for (var i = 0; i < d.stats.Count; i++)
                if (d.stats[i].revealed)
                    product.Reveal(i);
            return product;
        }

        private static ItemDef Item(RunDef def, string id) =>
            def.FindItem(id) ?? throw new System.FormatException($"Không có vật phẩm '{id}'.");

        /// <summary>Id rỗng = null; id không có trong RunDef = save hỏng.</summary>
        private static T Find<T>(IReadOnlyList<T> list, string id, System.Func<T, string> getId) where T : class
        {
            if (string.IsNullOrEmpty(id))
                return null;
            foreach (var x in list)
                if (getId(x) == id)
                    return x;
            throw new System.FormatException($"Không có '{id}' trong RunDef.");
        }

        private static long Bits(double v) => System.BitConverter.DoubleToInt64Bits(v);

        private static double Double(long bits) => System.BitConverter.Int64BitsToDouble(bits);
    }
}
