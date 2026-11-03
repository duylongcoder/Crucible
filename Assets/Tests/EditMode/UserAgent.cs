using System.Collections.Generic;
using System.Linq;
using Alchemy.Core;

namespace Alchemy.Tests.EditMode
{
    /// <summary>
    /// Agent đóng vai người chơi thật: không biết công thức, khoảng cách, Quality hay Power. Tự thử ghép nguyên liệu,
    /// nhớ những lần nung thành công qua các run (như người chơi ghi chép), chỉ thấy tên, tier, mức sao. Giữ trạng thái.
    /// </summary>
    internal sealed class UserAgent
    {
        public const string GapId = "_";
        private const int MaxGaps = 3;

        private readonly System.Random _random;

        // Trí nhớ qua các run: bố cục (id theo ô, "_" = trống) → kết quả.
        private readonly Dictionary<string, string> _tried = new Dictionary<string, string>();
        private readonly List<(string[] layout, ItemDef product)> _known = new List<(string[], ItemDef)>();

        private string[] _plan;

        public UserAgent(int seed) => _random = new System.Random(seed);

        /// <summary>Bố cục vừa gửi Nung (để người gọi ghi log); null nếu chưa nung.</summary>
        public string[] LastBrewLayout { get; private set; }

        public int KnownRecipes => _known.Select(k => k.product.Id).Distinct().Count();

        public ICommand Next(RunState s)
        {
            if (s.Phase != RunPhase.Placing)
                _plan = null;
            switch (s.Phase)
            {
                case RunPhase.Market: return Market(s);
                case RunPhase.Placing: return Placing(s);
                case RunPhase.Offering: return new OfferCommand(PickOffering(s));
                case RunPhase.Spoils: return new ChooseRewardCommand(PickReward(s));
                default: return new ContinueCommand();
            }
        }

        /// <summary>Ghi nhớ kết quả lượt nung vừa xong.</summary>
        public void Remember(string[] layout, ItemDef result)
        {
            var key = Key(layout);
            _tried[key] = result.Id;
            if (result.IsBrewed && !_known.Any(k => Key(k.layout) == key))
                _known.Add((layout, result));
        }

        private ICommand Market(RunState s)
        {
            for (var i = 0; i < s.Bag.Count; i++)
                if (s.Bag[i].Def.Kind == ItemKind.Scrap || s.Bag[i].Def.Kind == ItemKind.Lens)
                    return new SellCommand(i);

            // Hết vốn và có nhiều thành phẩm: bán cái tier thấp nhất.
            var brewed = s.Bag.Where(p => p.Def.IsBrewed).OrderBy(p => p.Def.Tier).ToList();
            if (s.Gold < 5 && brewed.Count > 2)
                return new SellCommand(IndexOf(s, brewed[0]));

            if (s.Bag.Count < s.BagCapacity - 1)
            {
                var knownIds = new HashSet<string>(_known.SelectMany(k => k.layout).Where(id => id != GapId));
                // Ưu tiên: nguyên liệu của công thức đã biết, rồi Xúc tác, rồi khoáng lạ rẻ để thử.
                var pick = Enumerable.Range(0, s.Offers.Count)
                    .Where(i => !s.Offers[i].Sold && s.Offers[i].Price <= s.Gold)
                    .Select(i => (i, o: s.Offers[i]))
                    .Where(x => x.o.Item.Kind == ItemKind.Mineral || x.o.Item.Kind == ItemKind.Additive
                                || x.o.Item.Kind == ItemKind.Catalyst && s.CatalystCount < s.Def.CatalystSlots && s.Gold - x.o.Price >= 4)
                    .OrderBy(x => knownIds.Contains(x.o.Item.Id) ? 0 : x.o.Item.Kind == ItemKind.Catalyst ? 1 : 2)
                    .ThenBy(x => Count(s, x.o.Item.Id))
                    .ThenBy(x => x.o.Price)
                    .Select(x => (int?)x.i)
                    .FirstOrDefault();
                if (pick.HasValue && (knownIds.Contains(s.Offers[pick.Value].Item.Id) || s.Gold >= 8))
                    return new BuyCommand(pick.Value);
            }

            return new LeaveMarketCommand();
        }

        private ICommand Placing(RunState s)
        {
            if (_plan == null)
                _plan = ChoosePlan(s);
            if (_plan == null)
                return new SkipBrewCommand();

            var w = s.Grid.Width;
            for (var c = 0; c < w * s.Grid.Height; c++)
            {
                var cell = s.Grid[c / w, c % w];
                var want = c < _plan.Length ? _plan[c] : GapId;
                if (cell != null && cell.Def.Id != want)
                    return new UnplaceCommand(c / w, c % w);
            }

            for (var c = 0; c < _plan.Length; c++)
            {
                if (_plan[c] == GapId || s.Grid[c / w, c % w] != null)
                    continue;
                var index = s.Bag.ToList().FindIndex(p => p.Def.Id == _plan[c]);
                if (index < 0)
                {
                    _plan = null;
                    return new SkipBrewCommand();
                }

                return new PlaceCommand(index, c / w, c % w);
            }

            LastBrewLayout = _plan;
            _plan = null;
            return new BrewCommand();
        }

        private string[] ChoosePlan(RunState s)
        {
            var have = s.Bag.Concat(s.Grid.ToFeedSequence().Where(p => p != null))
                .GroupBy(p => p.Def.Id).ToDictionary(g => g.Key, g => g.Count());

            // Đôi khi (30%) tò mò thử cái mới dù đã có cách nung quen.
            var explore = _random.Next(100) < 30;
            if (!explore)
            {
                var known = KnownPlan(s, have);
                if (known != null)
                    return known;
            }

            return ExplorePlan(s, have) ?? KnownPlan(s, have);
        }

        /// <summary>Bố cục đã từng ra thành phẩm tier cao nhất mà Túi đủ; thử thêm phụ gia vào khoảng trống nếu chưa thử.</summary>
        private string[] KnownPlan(RunState s, Dictionary<string, int> have)
        {
            foreach (var (layout, _) in _known.OrderByDescending(k => k.product.Tier))
            {
                if (!Enough(layout, have))
                    continue;
                var additive = have.Keys.Select(id => s.Def.FindItem(id)).FirstOrDefault(d => d.Kind == ItemKind.Additive);
                var gap = System.Array.IndexOf(layout, GapId);
                if (additive != null && gap >= 0)
                {
                    var withAdditive = (string[])layout.Clone();
                    withAdditive[gap] = additive.Id;
                    if (!_tried.ContainsKey(Key(withAdditive)) && Enough(withAdditive, have))
                        return withAdditive;
                }

                return layout;
            }

            return null;
        }

        /// <summary>Thử ghép 2–3 vật phẩm có trong Túi với 0–3 ô trống đều nhau, theo thứ tự chưa từng thử.</summary>
        private string[] ExplorePlan(RunState s, Dictionary<string, int> have)
        {
            // Chỉ khoáng và phụ gia cho vào lò được (Xúc tác, Kính soi, phế phẩm thì không). Giữ lại thành phẩm duy nhất
            // để có cái dâng, như người chơi cẩn thận.
            var brewedCount = s.Bag.Count(p => p.Def.IsBrewed);
            var ids = have.Keys.Where(id => s.Def.FindItem(id) is ItemDef d && (d.Kind == ItemKind.Mineral || d.Kind == ItemKind.Additive)
                                            && (!d.IsBrewed || brewedCount > 1))
                .OrderBy(id => id, System.StringComparer.Ordinal).ToList();
            var candidates = new List<string[]>();
            foreach (var a in ids)
            foreach (var b in ids)
            {
                for (var g = 0; g <= MaxGaps; g++)
                    candidates.Add(Layout(g, a, b));
                foreach (var c in ids)
                    for (var g = 0; g <= 1; g++)
                        candidates.Add(Layout(g, a, b, c));
            }

            var fresh = candidates.Where(l => l.Length <= 9 && Enough(l, have) && !_tried.ContainsKey(Key(l))).ToList();

            // Suy luận theo luật game dạy: cùng nguyên liệu, khác khoảng cách → khác thành phẩm. Thử biến thể của
            // bố cục đã thành công trước, rồi cặp 2 nguyên liệu, cuối cùng bộ 3.
            var knownSequences = new HashSet<string>(_known.Select(k => string.Join(",", k.layout.Where(id => id != GapId))));
            var variants = fresh.Where(l => knownSequences.Contains(string.Join(",", l.Where(id => id != GapId)))).ToList();
            var pairs = fresh.Where(l => l.Count(id => id != GapId) == 2).ToList();
            var pool = variants.Count > 0 ? variants : pairs.Count > 0 ? pairs : fresh;
            return pool.Count == 0 ? null : pool[_random.Next(pool.Count)];
        }

        private int PickOffering(RunState s)
        {
            // Người chơi chỉ thấy tier và mức sao: đối thủ dễ (≤ 2 sao) thì dâng thứ tier thấp nhất để dành đồ tốt.
            var brewed = Enumerable.Range(0, s.Bag.Count).Where(i => s.Bag[i].Def.IsBrewed)
                .OrderBy(i => s.Bag[i].Def.Tier).ToList();
            return Forge.Stars(s) <= 2 ? brewed.First() : brewed.Last();
        }

        private static int PickReward(RunState s)
        {
            int Rank(RewardDef r) => r.Kind == RewardKind.RareMineral ? 0 : r.Kind == RewardKind.Catalyst ? 1 : 2;
            var best = 0;
            for (var i = 1; i < s.RewardChoices.Count; i++)
                if (Rank(s.RewardChoices[i]) < Rank(s.RewardChoices[best]))
                    best = i;
            return best;
        }

        private static string[] Layout(int gaps, params string[] ids)
        {
            var list = new List<string>();
            for (var i = 0; i < ids.Length; i++)
            {
                if (i > 0)
                    for (var g = 0; g < gaps; g++)
                        list.Add(GapId);
                list.Add(ids[i]);
            }

            return list.ToArray();
        }

        private static bool Enough(string[] layout, Dictionary<string, int> have) =>
            layout.Where(id => id != GapId).GroupBy(id => id).All(g => have.TryGetValue(g.Key, out var n) && n >= g.Count());

        public static string Key(string[] layout) => string.Join(",", layout);

        private static int Count(RunState s, string id) => s.Bag.Count(p => p.Def.Id == id);

        private static int IndexOf(RunState s, Product p)
        {
            for (var i = 0; i < s.Bag.Count; i++)
                if (s.Bag[i] == p)
                    return i;
            return -1;
        }
    }
}
