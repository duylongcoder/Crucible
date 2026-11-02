using System.Collections.Generic;
using Alchemy.Core;

namespace Alchemy.Content
{
    /// <summary>
    /// Content tạm cho greybox M3 và harness M2. Số liệu cân bằng là tạm.
    /// </summary>
    // shortcut: content viết bằng code; chuyển sang ScriptableObject + validator ở M4.
    public static class DemoContent
    {
        public const string Sharpness = "sharpness";
        public const string Thorns = "thorns";
        public const string Absorb = "absorb";

        private static readonly Dictionary<string, string> StatNames = new Dictionary<string, string>
        {
            { StatIds.Quality, "Chất lượng" },
            { StatIds.Magic, "Kích hoạt ma thuật" },
            { Sharpness, "Sắc bén" },
            { Thorns, "Phản đòn" },
            { Absorb, "Hấp thụ sát thương" },
        };

        public static string StatName(string statId) => StatNames.TryGetValue(statId, out var name) ? name : statId;

        public static RunDef CreateRun()
        {
            ItemDef Raw(string id, string name, int tier, int price) =>
                new ItemDef(id, ItemKind.Mineral, tier, name: name, marketPrice: price);
            ItemDef Additive(string id, string name, int tier, int price, int magic) =>
                new ItemDef(id, ItemKind.Additive, tier, magicChance: magic, name: name, marketPrice: price);
            ItemDef Lens(string id, string name, int tier, int price, int lines) =>
                new ItemDef(id, ItemKind.Lens, tier, name: name, marketPrice: price, revealLines: lines);
            ItemDef Brewed(string id, string name, int tier, int price, Recipe recipe) =>
                new ItemDef(id, ItemKind.Mineral, tier, recipe, name: name, marketPrice: price);
            RecipeSlot I(string id, bool magic = false) => new RecipeSlot(id, magic);
            var gap = RecipeSlot.Gap;

            var iron = Raw("iron_ore", "Quặng sắt", 1, 4);
            var coal = Raw("coal", "Than đá", 1, 2);
            var copper = Raw("copper_ore", "Quặng đồng", 1, 4);
            var tin = Raw("tin", "Thiếc", 2, 6);
            var silver = Raw("silver", "Bạc", 3, 12);
            var borax = Additive("borax", "Hàn the", 1, 4, 10);
            var quartz = Additive("quartz_dust", "Bột thạch anh", 2, 8, 20);
            var cinnabar = Additive("cinnabar", "Thần sa", 3, 14, 30);
            var lens = Lens("lens_basic", "Kính soi thường", 1, 6, 1);
            var lensFine = Lens("lens_fine", "Kính soi tinh", 2, 12, 3);
            var meteorite = Raw("meteorite", "Thiên thạch", 4, 25);
            var obsidian = Raw("obsidian", "Hắc diện thạch", 5, 40);

            var pigIron = Brewed("pig_iron", "Gang", 2, 8, new Recipe(
                new[] { I("iron_ore"), I("coal") }, 6));
            var steel = Brewed("steel", "Thép", 3, 16, new Recipe(
                new[] { I("iron_ore"), gap, I("coal") }, 10,
                new[] { "borax", "quartz_dust" },
                new[] { new RecipeStat(Sharpness, 60, 4), new RecipeStat(Thorns, 30, 3) }));
            var bronze = Brewed("bronze", "Đồng thau", 3, 18, new Recipe(
                new[] { I("copper_ore"), gap, gap, I("tin") }, 12,
                new[] { "borax", "quartz_dust" },
                new[] { new RecipeStat(Absorb, 60, 4), new RecipeStat(Thorns, 20, 2) }));
            var temperedSteel = Brewed("tempered_steel", "Thép tôi", 5, 50, new Recipe(
                new[] { I("steel"), gap, I("coal"), gap, I("steel") }, 30,
                new[] { "borax", "quartz_dust", "cinnabar" },
                new[] { new RecipeStat(Sharpness, 80, 8), new RecipeStat(Thorns, 50, 5) }));
            var silverAlloy = Brewed("silver_alloy", "Hợp kim bạc", 5, 45, new Recipe(
                new[] { I("bronze"), gap, I("silver") }, 28,
                new[] { "quartz_dust", "cinnabar" },
                new[] { new RecipeStat(Absorb, 70, 7), new RecipeStat(Thorns, 50, 4) }));
            var starSteel = Brewed("star_steel", "Thép thiên thạch", 6, 60, new Recipe(
                new[] { I("meteorite"), gap, I("steel") }, 45,
                new[] { "cinnabar" },
                new[] { new RecipeStat(Sharpness, 70, 10), new RecipeStat(Absorb, 50, 6) }));
            var obsidianPlate = Brewed("obsidian_plate", "Giáp hắc diện", 6, 70, new Recipe(
                new[] { I("obsidian"), gap, gap, I("silver_alloy") }, 55,
                new[] { "cinnabar" },
                new[] { new RecipeStat(Absorb, 80, 12), new RecipeStat(Thorns, 60, 8) }));
            var trueIron = Brewed("true_iron", "Tinh thiết", 7, 150, new Recipe(
                new[] { I("tempered_steel", magic: true), gap, gap, I("silver") }, 70,
                new[] { "cinnabar" },
                new[] { new RecipeStat(Sharpness, 90, 15), new RecipeStat(Thorns, 60, 10) }));
            var supreme = Brewed("supreme_arcane", "Khoáng thạch Ma thuật Tối thượng", 10, 800, new Recipe(
                new[] { I("true_iron", magic: true), gap, I("silver_alloy", magic: true) }, 300,
                new[] { "cinnabar" },
                new[] { new RecipeStat(Sharpness, 100, 30), new RecipeStat(Thorns, 100, 30), new RecipeStat(Absorb, 100, 30) }));

            var scrap = new ItemDef("scrap", ItemKind.Scrap, 0, name: "Phế phẩm");

            var sword = new EquipmentDef("sword", "Kiếm", 1, new[] { new StatWeight(Sharpness, 2) });
            var spear = new EquipmentDef("spear", "Giáo", 1, new[] { new StatWeight(Sharpness, 1), new StatWeight(Thorns, 1) });
            var armor = new EquipmentDef("armor", "Giáp", 1, new[] { new StatWeight(Absorb, 2) });
            var shield = new EquipmentDef("shield", "Khiên", 1, new[] { new StatWeight(Thorns, 1), new StatWeight(Absorb, 1) });

            return new RunDef
            {
                Id = "demo",
                Brew = new BrewRules(3, 3, magicCapPercent: 90, magicPercentPerTier: 5),
                SeasonsPerRun = 8,
                BrewsPerSeason = 3,
                StartReputation = 3,
                StartGold = 0,
                BagCapacity = 14,
                MaxBagCapacity = 24,
                ShopMaxTier = 3,
                ShopTierWeights = new[] { 70, 25, 5 },
                ShopOfferCount = 6,
                RerollBaseCost = 2,
                RerollCostStep = 1,
                PriceFactorMinPercent = 70,
                PriceFactorMaxPercent = 130,
                SellPercent = 60,
                ScrapSellPrice = 1,
                ModifierChancePercent = 60,
                ModifierPercentMin = 50,
                ModifierPercentMax = 300,
                QualityWeight = 1,
                StarThresholds = new[] { 10.0, 20.0, 35.0, 55.0 },
                Scrap = scrap,
                Items = new[]
                {
                    iron, coal, copper, tin, silver, borax, quartz, cinnabar, lens, lensFine, meteorite, obsidian,
                    pigIron, steel, bronze, temperedSteel, silverAlloy, starSteel, obsidianPlate, trueIron, supreme,
                },
                ShopPool = new[] { iron, coal, copper, tin, silver, borax, quartz, cinnabar, lens, lensFine },
                StarterKit = new[] { iron, iron, iron, iron, coal, coal, coal, coal, copper, copper, tin, borax, lens },
                Opponents = new[]
                {
                    new OpponentDef("village_knight", "Hiệp sĩ làng Đông", BattleKind.Tournament, 6),
                    new OpponentDef("river_guard", "Vệ binh bến sông", BattleKind.Tournament, 9),
                    new OpponentDef("bandit_lord", "Thủ lĩnh sơn tặc", BattleKind.War, 13),
                    new OpponentDef("west_champion", "Quán quân trấn Tây", BattleKind.Tournament, 18),
                    new OpponentDef("border_army", "Quân biên ải", BattleKind.War, 26),
                    new OpponentDef("iron_legion", "Thiết đoàn phương Nam", BattleKind.War, 36),
                    new OpponentDef("royal_duelist", "Kiếm khách ngự tiền", BattleKind.Tournament, 50),
                    new OpponentDef("northern_host", "Đại quân phương Bắc", BattleKind.GrandWar, 75),
                },
                Equipment = new[] { sword, spear, armor, shield },
                Rewards = new[]
                {
                    new RewardDef("bag_2", "Nới Túi +2", RewardKind.BagExpand, amount: 2),
                    new RewardDef("bag_3", "Nới Túi +3", RewardKind.BagExpand, amount: 3, minSeason: 4),
                    new RewardDef("rare_meteorite", "Khoáng hiếm: Thiên thạch", RewardKind.RareMineral, meteorite),
                    new RewardDef("rare_obsidian", "Khoáng hiếm: Hắc diện thạch", RewardKind.RareMineral, obsidian, minSeason: 4),
                    new RewardDef("rare_silver", "Bạc ròng", RewardKind.RareMineral, silver),
                },
                Modifiers = new[]
                {
                    new SeasonModifierDef("rainy", "Mùa mưa", "Giá mua +20%",
                        new[] { new ParamScale(ScaledParam.BuyPrice, 120) }),
                    new SeasonModifierDef("fair", "Hội chợ", "Giá bán +25%",
                        new[] { new ParamScale(ScaledParam.SellPrice, 125) }),
                    new SeasonModifierDef("lens_shortage", "Thợ kính nghỉ", "Kính soi đắt gấp rưỡi",
                        new[] { new ParamScale(ScaledParam.LensPrice, 150) }),
                    new SeasonModifierDef("market_day", "Chợ phiên", "1 lần đổi hàng miễn phí mỗi lượt", freeRerolls: 1),
                    new SeasonModifierDef("weak_enemy", "Địch suy yếu", "Đối thủ yếu đi",
                        new[] { new ParamScale(ScaledParam.OpponentPower, 90) }),
                    new SeasonModifierDef("blade_forge", "Lò rèn ưa kiếm", "Dễ rèn ra Kiếm hơn",
                        new[] { new ParamScale(ScaledParam.EquipmentWeight, 300, "sword") }),
                },
            };
        }
    }
}
