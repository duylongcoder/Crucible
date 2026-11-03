using System.Collections.Generic;

namespace Alchemy.Core
{
    internal static class ProductFactory
    {
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
        /// Tạo thành phẩm cho một chuỗi nạp đã khớp <paramref name="def"/>, áp hiệu ứng OnRecipeMatch đã cộng dồn trong
        /// <paramref name="effects"/>. Luôn tung ma thuật đúng 1 lần rồi mỗi dòng thuộc tính 1 lần, nên phụ gia chỉ
        /// đổi ngưỡng ma thuật, không làm lệch các lần tung sau (I18).
        /// </summary>
        public static Product Create(ItemDef def, IReadOnlyList<Product> feed, BrewRules rules, Pcg32 chance,
            EffectContext effects = null)
        {
            effects = effects ?? new EffectContext();
            var recipe = def.Recipe;
            var essences = new List<double> { recipe.BaseEssence };
            essences.AddRange(effects.Essence);

            var essence = 0.0;
            foreach (var e in essences)
                essence += e;
            essence = ScoreCalculator.Clamp(essence, 0);
            var potency = ScoreCalculator.Potency(effects.AddPotency, effects.MulPotency);
            var quality = ScoreCalculator.Quality(essences, effects.AddPotency, effects.MulPotency);

            var stats = new List<StatLine> { new StatLine(StatIds.Quality, quality) };
            if (chance.NextInt(100) < MagicChance(feed, rules))
                stats.Add(new StatLine(StatIds.Magic, 1));
            foreach (var s in recipe.Stats)
                if (chance.NextInt(100) < s.ChancePercent)
                    stats.Add(new StatLine(s.StatId, ScoreCalculator.Clamp(s.Value, 0)));
            foreach (var s in effects.Stats)
                AddStat(stats, s);

            return new Product(def, essence, potency, stats);
        }

        // AddStat cộng vào dòng sẵn có, hoặc thêm dòng mới (vẫn ẩn tới khi soi).
        private static void AddStat(List<StatLine> stats, StatLine add)
        {
            for (var i = 0; i < stats.Count; i++)
            {
                if (stats[i].StatId != add.StatId)
                    continue;
                stats[i] = new StatLine(add.StatId, ScoreCalculator.Clamp(stats[i].Value + add.Value, 0));
                return;
            }

            stats.Add(new StatLine(add.StatId, ScoreCalculator.Clamp(add.Value, 0)));
        }
    }
}
