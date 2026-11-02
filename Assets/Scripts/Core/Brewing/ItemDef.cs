using System.Collections.Generic;

namespace Alchemy.Core
{
    public enum ItemKind
    {
        Mineral,
        Additive,
        Lens,
        Catalyst,
        Reagent,
        // ScrapDef của Content quy về đây: một def chung, không bao giờ là nguyên liệu (I25).
        Scrap,
    }

    /// <summary>Data immutable của một loại vật phẩm. Content chuyển ScriptableObject sang đây.</summary>
    public sealed class ItemDef
    {
        public string Id { get; }
        public ItemKind Kind { get; }
        public int Tier { get; }

        /// <summary>Null = nguyên liệu gốc. Thành phẩm của công thức là chính item này.</summary>
        public Recipe Recipe { get; }

        /// <summary>Chỉ phụ gia: phần trăm cộng vào xác suất ma thuật khi đứng trong khoảng trống.</summary>
        public int MagicChance { get; }

        public ItemDef(string id, ItemKind kind, int tier, Recipe recipe = null, int magicChance = 0)
        {
            Id = id;
            Kind = kind;
            Tier = tier;
            Recipe = recipe;
            MagicChance = magicChance;
        }
    }

    /// <summary>Một slot của <see cref="Recipe.Pattern"/>: nguyên liệu (<see cref="ItemId"/>) hoặc khoảng trống.</summary>
    public sealed class RecipeSlot
    {
        public static readonly RecipeSlot Gap = new RecipeSlot(null, false);

        /// <summary>Null = khoảng trống.</summary>
        public string ItemId { get; }
        public bool RequiresMagic { get; }

        public bool IsGap => ItemId == null;

        public RecipeSlot(string itemId, bool requiresMagic = false)
        {
            ItemId = itemId;
            RequiresMagic = requiresMagic;
        }
    }

    /// <summary>Dòng thuộc tính khởi đầu của thành phẩm: xuất hiện với xác suất <see cref="ChancePercent"/> (stream Chance).</summary>
    public sealed class RecipeStat
    {
        public string StatId { get; }
        public int ChancePercent { get; }
        public double Value { get; }

        public RecipeStat(string statId, int chancePercent, double value)
        {
            StatId = statId;
            ChancePercent = chancePercent;
            Value = value;
        }
    }

    public sealed class Recipe
    {
        /// <summary>Slot đầu và cuối luôn là nguyên liệu, độ dài ≤ số ô lưới (validator kiểm).</summary>
        public IReadOnlyList<RecipeSlot> Pattern { get; }
        public IReadOnlyList<string> AllowedAdditives { get; }
        public double BaseEssence { get; }
        public IReadOnlyList<RecipeStat> Stats { get; }

        public Recipe(IReadOnlyList<RecipeSlot> pattern, double baseEssence,
            IReadOnlyList<string> allowedAdditives = null, IReadOnlyList<RecipeStat> stats = null)
        {
            Pattern = pattern;
            BaseEssence = baseEssence;
            AllowedAdditives = allowedAdditives ?? System.Array.Empty<string>();
            Stats = stats ?? System.Array.Empty<RecipeStat>();
        }
    }
}
