using Alchemy.Core;

namespace Alchemy.Tests.EditMode
{
    /// <summary>Content nhỏ dựng bằng code cho test Core. Không phải content thật của game.</summary>
    public static class TestContent
    {
        public static readonly BrewRules Rules = new BrewRules(3, 3, magicCapPercent: 90, magicPercentPerTier: 5);

        public static readonly ItemDef Iron = new ItemDef("iron", ItemKind.Mineral, 1);
        public static readonly ItemDef Coal = new ItemDef("coal", ItemKind.Mineral, 1);
        public static readonly ItemDef Copper = new ItemDef("copper", ItemKind.Mineral, 2);

        public static readonly ItemDef Flux = new ItemDef("flux", ItemKind.Additive, 1, magicChance: 10);
        public static readonly ItemDef Salt = new ItemDef("salt", ItemKind.Additive, 2, magicChance: 20);
        public static readonly ItemDef Sulfur = new ItemDef("sulfur", ItemKind.Additive, 3, magicChance: 30);

        public static readonly ItemDef Scrap = new ItemDef("scrap", ItemKind.Scrap, 0);

        // Steel và Bronze: cùng nguyên liệu, cùng thứ tự, khác khoảng cách.
        public static readonly ItemDef Steel = new ItemDef("steel", ItemKind.Mineral, 4, new Recipe(
            new[] { new RecipeSlot("iron"), RecipeSlot.Gap, new RecipeSlot("coal") },
            baseEssence: 6,
            allowedAdditives: new[] { "flux", "salt" },
            stats: new[] { new RecipeStat("sharpness", 50, 2) }));

        public static readonly ItemDef Bronze = new ItemDef("bronze", ItemKind.Mineral, 3, new Recipe(
            new[] { new RecipeSlot("iron"), RecipeSlot.Gap, RecipeSlot.Gap, new RecipeSlot("coal") },
            baseEssence: 8));

        public static readonly ItemDef Arcanite = new ItemDef("arcanite", ItemKind.Mineral, 6, new Recipe(
            new[] { new RecipeSlot("steel", requiresMagic: true), RecipeSlot.Gap, new RecipeSlot("copper") },
            baseEssence: 20,
            allowedAdditives: new[] { "sulfur" }));

        public static readonly ItemDef[] Items = { Iron, Coal, Copper, Flux, Salt, Sulfur, Steel, Bronze, Arcanite };

        public static Brewer CreateBrewer() => new Brewer(Rules, Items, Scrap);

        public static Product Raw(ItemDef def) => new Product(def);

        public static Product Magic(ItemDef def) =>
            new Product(def, stats: new[] { new StatLine(StatIds.Magic, 1) });

        /// <summary>Lưới 3×3 điền theo thứ tự quét hàng; null = ô trống.</summary>
        public static Grid MakeGrid(params Product[] rowMajor)
        {
            var grid = new Grid(Rules.GridWidth, Rules.GridHeight);
            for (var i = 0; i < rowMajor.Length; i++)
                if (rowMajor[i] != null)
                    grid.Place(i / grid.Width, i % grid.Width, rowMajor[i]);
            return grid;
        }

        public static BrewResult Brew(ulong seed, params Product[] rowMajor) =>
            CreateBrewer().Brew(MakeGrid(rowMajor), new RngStreams(seed));
    }
}
