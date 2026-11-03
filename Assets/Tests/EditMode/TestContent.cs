using Alchemy.Content;
using Alchemy.Core;
using UnityEditor;

namespace Alchemy.Tests.EditMode
{
    /// <summary>Content nhỏ dựng bằng code cho test Core. Không phải content thật của game.</summary>
    internal static class TestContent
    {
        public static readonly BrewRules Rules = new BrewRules(3, 3, magicCapPercent: 90, magicPercentPerTier: 5);

        public static readonly ItemDef Iron = new ItemDef("iron", ItemKind.Mineral, 1, marketPrice: 4);
        public static readonly ItemDef Coal = new ItemDef("coal", ItemKind.Mineral, 1, marketPrice: 2);
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
            stats: new[] { new RecipeStat("sharpness", 50, 2) }), marketPrice: 10);

        public static readonly ItemDef Bronze = new ItemDef("bronze", ItemKind.Mineral, 3, new Recipe(
            new[] { new RecipeSlot("iron"), RecipeSlot.Gap, RecipeSlot.Gap, new RecipeSlot("coal") },
            baseEssence: 8));

        public static readonly ItemDef Arcanite = new ItemDef("arcanite", ItemKind.Mineral, 6, new Recipe(
            new[] { new RecipeSlot("steel", requiresMagic: true), RecipeSlot.Gap, new RecipeSlot("copper") },
            baseEssence: 20,
            allowedAdditives: new[] { "sulfur" }));

        public static readonly ItemDef Lens = new ItemDef("lens", ItemKind.Lens, 1, marketPrice: 3, revealLines: 1);
        public static readonly ItemDef Meteorite = new ItemDef("meteorite", ItemKind.Mineral, 4, marketPrice: 20);

        public static readonly ItemDef[] Items = { Iron, Coal, Copper, Flux, Salt, Sulfur, Steel, Bronze, Arcanite, Lens, Meteorite };

        /// <summary>
        /// Run nhỏ: đối thủ mọi Mùa có Power = Quality của Steel (6); trang bị không có trọng số thuộc tính
        /// nên Power trang bị = Quality. ShopPool cố ý chứa Steel (có Recipe) và Meteorite (tier 4) để kiểm I24.
        /// </summary>
        public static RunDef CreateRun(double opponentPower = 6)
        {
            var opponents = new OpponentDef[8];
            for (var i = 0; i < opponents.Length; i++)
                opponents[i] = new OpponentDef("opp" + i, "Đối thủ " + i, BattleKind.Tournament, opponentPower);

            return new RunDef
            {
                Id = "test",
                Brew = Rules,
                SeasonsPerRun = 8,
                BrewsPerSeason = 3,
                StartReputation = 3,
                StartGold = 0,
                BagCapacity = 10,
                MaxBagCapacity = 14,
                CatalystSlots = 5,
                ShopMaxTier = 3,
                ShopTierWeights = new[] { 70, 25, 5 },
                ShopOfferCount = 5,
                RerollBaseCost = 2,
                RerollCostStep = 1,
                PriceFactorMinPercent = 70,
                PriceFactorMaxPercent = 130,
                SellPercent = 50,
                ScrapSellPrice = 1,
                ModifierChancePercent = 0,
                ModifierPercentMin = 50,
                ModifierPercentMax = 300,
                QualityWeight = 1,
                StarThresholds = new[] { 10.0, 20.0, 35.0, 55.0 },
                Items = Items,
                Scrap = Scrap,
                ShopPool = new[] { Iron, Coal, Copper, Flux, Salt, Sulfur, Lens, Steel, Meteorite },
                StarterKits = new[] { Kit(Iron, Coal, Lens) },
                Difficulties = new[] { new DifficultyDef("normal", "Thường", opponents) },
                RenownPerWin = 10,
                RenownPerTier = 5,
                RenownRunWin = 30,
                Equipment = new[] { new EquipmentDef("plain", "Gậy", 1, new StatWeight[0]) },
                Rewards = new[]
                {
                    new RewardDef("bag", "Nới Túi", RewardKind.BagExpand, amount: 2),
                    new RewardDef("rare", "Thiên thạch", RewardKind.RareMineral, Meteorite),
                },
                Modifiers = new SeasonModifierDef[0],
            };
        }

        public static StarterKitDef Kit(params ItemDef[] items) => new StarterKitDef("kit", "Bộ thử", items);

        public static DifficultyDef Difficulty(params OpponentDef[] opponents) => new DifficultyDef("normal", "Thường", opponents);

        public const string MvpRunPath = "Assets/Content/MvpRun.asset";

        public static RunDefAsset MvpRunAsset() => AssetDatabase.LoadAssetAtPath<RunDefAsset>(MvpRunPath);

        /// <summary>Content MVP thật (Assets/Content), đã chuyển sang data của Core.</summary>
        public static RunDef MvpRun() => ContentConverter.ToRunDef(MvpRunAsset());

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
