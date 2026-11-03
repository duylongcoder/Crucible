using System.Collections.Generic;

namespace Alchemy.Core
{
    /// <summary>
    /// Whitelist Trigger MVP. OnPlace bỏ vì gỡ/đặt lại tự do sẽ kích hoạt lặp vô hạn; OnSeasonStart để sau MVP.
    /// </summary>
    public enum EffectTrigger
    {
        OnBrewStart,
        OnRecipeMatch,
        OnBrewFail,
        OnBrewEnd,
        OnShopEnter,
    }

    public enum EffectConditionKind
    {
        /// <summary>Lưới có nguyên liệu <see cref="EffectCondition.ItemId"/>.</summary>
        IngredientIs,
        /// <summary>Thành phẩm khớp là <see cref="EffectCondition.ItemId"/>.</summary>
        RecipeIs,
        /// <summary>Số phụ gia trên lưới ≥ <see cref="EffectCondition.Amount"/>.</summary>
        AdditiveCount,
        /// <summary>Tung stream Chance, đạt khi &lt; <see cref="EffectCondition.Amount"/> %.</summary>
        Chance,
    }

    /// <summary>AddMagicChance của phụ gia là <see cref="ItemDef.MagicChance"/>, không chạy qua runner.</summary>
    public enum EffectAction
    {
        AddEssence,
        AddPotency,
        MulPotency,
        AddStat,
        GainGold,
        CreateIngredient,
    }

    public sealed class EffectCondition
    {
        internal EffectConditionKind Kind { get; }
        internal string ItemId { get; }
        internal int Amount { get; }

        public EffectCondition(EffectConditionKind kind, string itemId = null, int amount = 0)
        {
            Kind = kind;
            ItemId = itemId;
            Amount = amount;
        }
    }

    /// <summary>Hiệu ứng = Trigger + Condition (mọi điều kiện phải đạt) + Action. Chỉ <see cref="EffectRunner"/> đọc.</summary>
    public sealed class EffectDef
    {
        internal EffectTrigger Trigger { get; }
        internal IReadOnlyList<EffectCondition> Conditions { get; }
        internal EffectAction Action { get; }
        internal double Value { get; }

        /// <summary>AddStat: dòng thuộc tính. CreateIngredient: item tạo ra.</summary>
        internal string TargetId { get; }

        public EffectDef(EffectTrigger trigger, EffectAction action, double value = 0, string targetId = null,
            IReadOnlyList<EffectCondition> conditions = null)
        {
            Trigger = trigger;
            Action = action;
            Value = value;
            TargetId = targetId;
            Conditions = conditions ?? System.Array.Empty<EffectCondition>();
        }
    }
}
