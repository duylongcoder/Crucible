using System.Collections.Generic;

namespace Alchemy.Core
{
    internal static class ProductFactory
    {
        private static readonly double[] None = System.Array.Empty<double>();

        /// <summary>
        /// Xác suất ma thuật (%) = truyền thừa (Tier × MagicPercentPerTier mỗi nguyên liệu đã kích hoạt)
        /// + MagicChance của mọi phụ gia, trần MagicCapPercent (I25).
        /// </summary>
        public static int MagicChance(IReadOnlyList<Product> feed, BrewRules rules)
        {
            var chance = 0;
            foreach (var cell in feed)
            {
                if (cell == null)
                    continue;
                if (cell.Def.Kind == ItemKind.Additive)
                    chance += cell.Def.MagicChance;
                else if (cell.IsMagic)
                    chance += cell.Def.Tier * rules.MagicPercentPerTier;
            }

            return chance < 0 ? 0 : chance > rules.MagicCapPercent ? rules.MagicCapPercent : chance;
        }

        /// <summary>
        /// Tạo thành phẩm cho một chuỗi nạp đã khớp <paramref name="def"/>. Luôn tung ma thuật đúng 1 lần rồi
        /// mỗi dòng thuộc tính 1 lần, nên phụ gia chỉ đổi ngưỡng ma thuật, không làm lệch các lần tung sau (I18).
        /// </summary>
        public static Product Create(ItemDef def, IReadOnlyList<Product> feed, BrewRules rules, Pcg32 chance)
        {
            var recipe = def.Recipe;
            // shortcut: chưa áp Effect của Xúc tác/Thuốc thử (Add/MulPotency, AddEssence); nâng cấp khi có EffectRunner (M2).
            var potency = ScoreCalculator.Potency(None, None);
            var essence = ScoreCalculator.Clamp(recipe.BaseEssence, 0);
            var quality = ScoreCalculator.Quality(new[] { essence }, None, None);

            var stats = new List<StatLine> { new StatLine(StatIds.Quality, quality) };
            if (chance.NextInt(100) < MagicChance(feed, rules))
                stats.Add(new StatLine(StatIds.Magic, 1));
            foreach (var s in recipe.Stats)
                if (chance.NextInt(100) < s.ChancePercent)
                    stats.Add(new StatLine(s.StatId, ScoreCalculator.Clamp(s.Value, 0)));

            return new Product(def, essence, potency, stats);
        }
    }
}
