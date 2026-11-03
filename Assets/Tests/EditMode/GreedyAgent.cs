using System.Collections.Generic;
using System.Linq;
using Alchemy.Core;

namespace Alchemy.Tests.EditMode
{
    /// <summary>
    /// Agent tham lam cho harness và báo cáo cân bằng (M6). Biết mọi công thức kể cả khoảng cách (như người chơi đã tra
    /// cộng đồng), nhìn được Quality và Power đối thủ (như người chơi giỏi đọc mức sao). Mỗi lần gọi trả về đúng 1
    /// Command; không giữ trạng thái riêng.
    /// </summary>
    internal static class GreedyAgent
    {
        // Lượng nguyên liệu gốc muốn có trong Túi trước khi nung.
        private static readonly Dictionary<string, int> Stock = new Dictionary<string, int>
        {
            { "iron_ore", 3 }, { "coal", 3 }, { "copper_ore", 1 }, { "tin", 1 }, { "silver", 2 },
            { "quartz_dust", 1 }, { "cinnabar", 2 }, { "gold_ore", 1 },
        };

        private const int KeepBrewed = 2;

        /// <summary>Dưới mức này agent bán khoáng không nằm trong <see cref="Stock"/> (khoáng hiếm chưa dùng tới) để có vốn.</summary>
        private const int LowGold = 10;

        public static ICommand Next(RunState s)
        {
            switch (s.Phase)
            {
                case RunPhase.Market: return Market(s);
                case RunPhase.Placing: return Placing(s);
                case RunPhase.Offering: return new OfferCommand(CheapestWinning(s));
                case RunPhase.Spoils: return new ChooseRewardCommand(PickReward(s));
                default: return new ContinueCommand();
            }
        }

        private static ICommand Market(RunState s)
        {
            // Bán: phế phẩm, Kính soi (agent không soi), thành phẩm yếu ngoài top KeepBrewed, nguyên liệu dư.
            for (var i = 0; i < s.Bag.Count; i++)
            {
                var p = s.Bag[i];
                if (p.Def.Kind == ItemKind.Scrap || p.Def.Kind == ItemKind.Lens)
                    return new SellCommand(i);
            }

            var brewed = s.Bag.Where(p => p.Def.IsBrewed).OrderByDescending(p => p.Quality).ToList();
            if (brewed.Count > KeepBrewed)
                return new SellCommand(IndexOf(s, brewed[brewed.Count - 1]));

            if (s.Gold < LowGold && BestPlan(s) == null)
                for (var i = 0; i < s.Bag.Count; i++)
                    if (s.Bag[i].Def.Kind == ItemKind.Mineral && !s.Bag[i].Def.IsBrewed && !Stock.ContainsKey(s.Bag[i].Def.Id))
                        return new SellCommand(i);

            foreach (var kv in Stock)
                if (Count(s, kv.Key) > kv.Value * 2)
                    return new SellCommand(s.Bag.ToList().FindIndex(p => p.Def.Id == kv.Key));

            // Mua: Xúc tác khi còn chỗ, rồi nguyên liệu đang thiếu nhiều nhất. Chừa 1 chỗ Túi.
            if (s.Bag.Count < s.BagCapacity - 1)
            {
                for (var i = 0; i < s.Offers.Count; i++)
                {
                    var o = s.Offers[i];
                    if (!o.Sold && o.Item.Kind == ItemKind.Catalyst && s.CatalystCount < s.Def.CatalystSlots && o.Price <= s.Gold)
                        return new BuyCommand(i);
                }

                var best = -1;
                var bestNeed = 0;
                for (var i = 0; i < s.Offers.Count; i++)
                {
                    var o = s.Offers[i];
                    if (o.Sold || o.Price > s.Gold || !Stock.TryGetValue(o.Item.Id, out var want))
                        continue;
                    var need = want - Count(s, o.Item.Id);
                    if (need > bestNeed)
                    {
                        best = i;
                        bestNeed = need;
                    }
                }

                if (best >= 0)
                    return new BuyCommand(best);
            }

            return new LeaveMarketCommand();
        }

        private static ICommand Placing(RunState s)
        {
            var plan = BestPlan(s);
            if (plan == null)
                return new SkipBrewCommand();

            var cells = s.Grid.Width * s.Grid.Height;
            for (var c = 0; c < cells; c++)
            {
                var cell = s.Grid[c / s.Grid.Width, c % s.Grid.Width];
                var want = c < plan.Count ? plan[c] : null;
                if (cell != null && cell != want)
                    return new UnplaceCommand(c / s.Grid.Width, c % s.Grid.Width);
            }

            for (var c = 0; c < plan.Count; c++)
                if (plan[c] != null && s.Grid[c / s.Grid.Width, c % s.Grid.Width] == null)
                    return new PlaceCommand(IndexOf(s, plan[c]), c / s.Grid.Width, c % s.Grid.Width);
            return new BrewCommand();
        }

        /// <summary>
        /// Công thức tier cao nhất làm được từ Túi + lưới hiện tại: danh sách vật phẩm theo ô (null = trống).
        /// Gap được lấp bằng phụ gia cho phép có MagicChance cao nhất để mở nhánh ma thuật.
        /// </summary>
        private static List<Product> BestPlan(RunState s)
        {
            // Vật phẩm đã trên lưới đứng trước: khi hòa thì giữ nguyên, không gỡ ra đặt lại mãi.
            var pool = s.Grid.ToFeedSequence().Where(p => p != null).Concat(s.Bag).ToList();
            foreach (var target in s.Def.Items.Where(i => i.IsBrewed).OrderByDescending(i => i.Tier))
            {
                var used = new HashSet<Product>();
                var plan = new List<Product>();
                var ok = true;
                foreach (var slot in target.Recipe.Pattern)
                {
                    if (slot.IsGap)
                    {
                        plan.Add(null);
                        continue;
                    }

                    var pick = pool.Where(p => !used.Contains(p) && p.Def.Id == slot.ItemId && (!slot.RequiresMagic || p.IsMagic))
                        .OrderBy(p => p.IsMagic && !slot.RequiresMagic ? 1 : 0).ThenBy(p => p.Quality).FirstOrDefault();
                    if (pick == null)
                    {
                        ok = false;
                        break;
                    }

                    used.Add(pick);
                    plan.Add(pick);
                }

                if (!ok)
                    continue;
                for (var k = 0; k < plan.Count; k++)
                {
                    if (plan[k] != null)
                        continue;
                    var additive = pool.Where(p => !used.Contains(p) && target.Recipe.AllowedAdditives.Contains(p.Def.Id))
                        .OrderByDescending(p => p.Def.MagicChance).FirstOrDefault();
                    if (additive == null)
                        break;
                    used.Add(additive);
                    plan[k] = additive;
                }

                return plan;
            }

            return null;
        }

        /// <summary>Thành phẩm Quality thấp nhất mà vẫn ≥ Power đối thủ (cận dưới, chưa tính thuộc tính); không có thì mạnh nhất.</summary>
        private static int CheapestWinning(RunState s)
        {
            var need = Forge.OpponentPower(s) / s.Def.QualityWeight;
            int cheapest = -1, best = -1;
            for (var i = 0; i < s.Bag.Count; i++)
            {
                var p = s.Bag[i];
                if (!p.Def.IsBrewed)
                    continue;
                if (best < 0 || p.Quality > s.Bag[best].Quality)
                    best = i;
                if (p.Quality >= need && (cheapest < 0 || p.Quality < s.Bag[cheapest].Quality))
                    cheapest = i;
            }

            return cheapest >= 0 ? cheapest : best;
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
