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
        public string Name { get; }
        public ItemKind Kind { get; }
        public int Tier { get; }
        public int MarketPrice { get; }

        /// <summary>Null = nguyên liệu gốc. Thành phẩm của công thức là chính item này.</summary>
        public Recipe Recipe { get; }

        /// <summary>Chỉ phụ gia: phần trăm cộng vào xác suất ma thuật khi đứng trong khoảng trống.</summary>
        public int MagicChance { get; }

        /// <summary>Chỉ Kính soi: số dòng thuộc tính tối đa lộ mỗi lần.</summary>
        public int RevealLines { get; }

        public ItemDef(string id, ItemKind kind, int tier, Recipe recipe = null, int magicChance = 0,
            string name = null, int marketPrice = 0, int revealLines = 0)
        {
            Id = id;
            Kind = kind;
            Tier = tier;
            Recipe = recipe;
            MagicChance = magicChance;
            Name = name ?? id;
            MarketPrice = marketPrice;
            RevealLines = revealLines;
        }

        /// <summary>Thành phẩm nung ra từ công thức: dâng được, làm nguyên liệu được.</summary>
        public bool IsBrewed => Recipe != null;
    }

    /// <summary>Một slot của <see cref="Recipe.Pattern"/>: nguyên liệu (<see cref="ItemId"/>) hoặc khoảng trống.</summary>
    public sealed class RecipeSlot
    {
        public static readonly RecipeSlot Gap = new RecipeSlot(null, false);

        /// <summary>Null = khoảng trống.</summary>
        internal string ItemId { get; }
        internal bool RequiresMagic { get; }

        internal bool IsGap => ItemId == null;

        public RecipeSlot(string itemId, bool requiresMagic = false)
        {
            ItemId = itemId;
            RequiresMagic = requiresMagic;
        }
    }

    /// <summary>Dòng thuộc tính khởi đầu của thành phẩm: xuất hiện với xác suất <see cref="ChancePercent"/> (stream Chance).</summary>
    public sealed class RecipeStat
    {
        internal string StatId { get; }
        internal int ChancePercent { get; }
        internal double Value { get; }

        public RecipeStat(string statId, int chancePercent, double value)
        {
            StatId = statId;
            ChancePercent = chancePercent;
            Value = value;
        }
    }

    /// <summary>
    /// Mọi thành phần là internal: khoảng cách, phụ gia, Essence, thuộc tính không được đi qua ranh giới
    /// Core → Presentation. Sổ công thức (M5) sẽ có API riêng chỉ lộ nguyên liệu + thứ tự.
    /// </summary>
    public sealed class Recipe
    {
        /// <summary>Slot đầu và cuối luôn là nguyên liệu, độ dài ≤ số ô lưới (validator kiểm).</summary>
        internal IReadOnlyList<RecipeSlot> Pattern { get; }
        internal IReadOnlyList<string> AllowedAdditives { get; }
        internal double BaseEssence { get; }
        internal IReadOnlyList<RecipeStat> Stats { get; }

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
