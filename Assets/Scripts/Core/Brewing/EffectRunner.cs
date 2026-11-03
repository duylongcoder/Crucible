using System.Collections.Generic;

namespace Alchemy.Core
{
    /// <summary>Kết quả cộng dồn của mọi hiệu ứng trong một lần kích hoạt; Core áp sau khi chạy xong.</summary>
    internal sealed class EffectContext
    {
        public readonly List<double> Essence = new List<double>();
        public readonly List<double> AddPotency = new List<double>();
        public readonly List<double> MulPotency = new List<double>();
        public readonly List<StatLine> Stats = new List<StatLine>();
        public readonly List<string> Created = new List<string>();
        public int Gold;
    }

    /// <summary>Nơi duy nhất biết về Effect primitive (CLAUDE.md mục 3).</summary>
    internal static class EffectRunner
    {
        /// <summary>Chạy hiệu ứng của mọi nguồn (Xúc tác trong Túi) theo thứ tự Túi.</summary>
        /// <param name="feed">Chuỗi nạp; null ngoài lượt nung.</param>
        /// <param name="matched">Thành phẩm khớp; null nếu chưa khớp hoặc hỏng.</param>
        public static void Run(EffectTrigger trigger, IReadOnlyList<Product> sources, IReadOnlyList<Product> feed,
            ItemDef matched, Pcg32 chance, EffectContext context)
        {
            if (sources == null)
                return;
            foreach (var source in sources)
                foreach (var effect in source.Def.Effects)
                    if (effect.Trigger == trigger && Passes(effect, feed, matched, chance))
                        Apply(effect, context);
        }

        // Tung đủ mọi điều kiện Chance, không dừng sớm: số lần tung chỉ phụ thuộc Trigger, nên phụ gia hợp lệ
        // không làm lệch stream Chance của các lần tung sau (I18).
        private static bool Passes(EffectDef effect, IReadOnlyList<Product> feed, ItemDef matched, Pcg32 chance)
        {
            var ok = true;
            foreach (var c in effect.Conditions)
                ok &= Check(c, feed, matched, chance);
            return ok;
        }

        private static bool Check(EffectCondition c, IReadOnlyList<Product> feed, ItemDef matched, Pcg32 chance)
        {
            switch (c.Kind)
            {
                case EffectConditionKind.IngredientIs:
                    return Count(feed, p => p.Def.Id == c.ItemId) > 0;
                case EffectConditionKind.RecipeIs:
                    return matched != null && matched.Id == c.ItemId;
                case EffectConditionKind.AdditiveCount:
                    return Count(feed, p => p.Def.Kind == ItemKind.Additive) >= c.Amount;
                case EffectConditionKind.Chance:
                    return chance.NextInt(100) < c.Amount;
                default:
                    return false;
            }
        }

        private static void Apply(EffectDef e, EffectContext context)
        {
            switch (e.Action)
            {
                case EffectAction.AddEssence:
                    context.Essence.Add(e.Value);
                    break;
                case EffectAction.AddPotency:
                    context.AddPotency.Add(e.Value);
                    break;
                case EffectAction.MulPotency:
                    context.MulPotency.Add(e.Value);
                    break;
                case EffectAction.AddStat:
                    context.Stats.Add(new StatLine(e.TargetId, e.Value));
                    break;
                case EffectAction.GainGold:
                    context.Gold += (int)e.Value;
                    break;
                case EffectAction.CreateIngredient:
                    context.Created.Add(e.TargetId);
                    break;
            }
        }

        private static int Count(IReadOnlyList<Product> feed, System.Func<Product, bool> match)
        {
            var n = 0;
            if (feed != null)
                foreach (var p in feed)
                    if (p != null && match(p))
                        n++;
            return n;
        }
    }
}
