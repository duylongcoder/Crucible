using System;
using System.Collections.Generic;
using Alchemy.Core;
using UnityEngine;

namespace Alchemy.Content
{
    /// <summary>Một loại vật phẩm (khoáng, phụ gia, Kính soi, Xúc tác, phế phẩm). Có công thức khi <see cref="RecipeData.pattern"/> không rỗng.</summary>
    [CreateAssetMenu(menuName = "Crucible/Item", fileName = "Item")]
    public sealed class ItemDefAsset : ScriptableObject
    {
        public string id;
        public string displayName;
        public ItemKind kind;
        [Range(0, 10)] public int tier = 1;
        [Min(0)] public int marketPrice;

        [Tooltip("Chỉ phụ gia: % cộng vào xác suất ma thuật (AddMagicChance).")]
        [Range(0, 50)] public int magicChance;

        [Tooltip("Chỉ Kính soi: số dòng thuộc tính lộ tối đa mỗi lần.")]
        [Min(0)] public int revealLines;

        [TextArea] public string description;

        public RecipeData recipe = new RecipeData();

        [Tooltip("Chỉ Xúc tác.")]
        public List<EffectData> effects = new List<EffectData>();

        public bool HasRecipe => recipe != null && recipe.pattern != null && recipe.pattern.Count > 0;
    }

    [Serializable]
    public sealed class RecipeData
    {
        [Tooltip("Slot có item = nguyên liệu; item trống = khoảng trống (Gap).")]
        public List<SlotData> pattern = new List<SlotData>();
        public List<ItemDefAsset> allowedAdditives = new List<ItemDefAsset>();
        public double baseEssence;
        public List<RecipeStatData> stats = new List<RecipeStatData>();
    }

    [Serializable]
    public sealed class SlotData
    {
        public ItemDefAsset item;
        public bool requiresMagic;
    }

    [Serializable]
    public sealed class RecipeStatData
    {
        public StatDefAsset stat;
        [Range(0, 100)] public int chancePercent;
        public double value;
    }

    [Serializable]
    public sealed class EffectData
    {
        public EffectTrigger trigger;
        public List<ConditionData> conditions = new List<ConditionData>();
        public EffectAction action;
        public double value;

        [Tooltip("AddStat.")]
        public StatDefAsset stat;

        [Tooltip("CreateIngredient.")]
        public ItemDefAsset item;
    }

    [Serializable]
    public sealed class ConditionData
    {
        public EffectConditionKind kind;

        [Tooltip("IngredientIs, RecipeIs.")]
        public ItemDefAsset item;

        [Tooltip("AdditiveCount: số phụ gia tối thiểu. Chance: %.")]
        public int amount;
    }
}
