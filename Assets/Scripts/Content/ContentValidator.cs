using System.Collections.Generic;
using Alchemy.Core;

namespace Alchemy.Content
{
    /// <summary>
    /// Kiểm content trước khi chạy (Enter Play Mode + EditMode test). Trả về danh sách lỗi; rỗng = hợp lệ.
    /// Lỗi chỉ dành cho người làm content, không bao giờ đi tới UI người chơi.
    /// </summary>
    public static class ContentValidator
    {
        public const int MaxTier = 10;
        public const double MaxMulPotency = 5;
        public const int MaxGoldPerEffect = 100;
        public const int MaxMagicChancePerAdditive = 50;

        public static List<string> Validate(RunDefAsset run)
        {
            var errors = new List<string>();
            if (run == null)
            {
                errors.Add("RunDef null.");
                return errors;
            }

            void Error(string message) => errors.Add($"[{run.name}] {message}");

            ValidateConfig(run, Error);
            if (HasNull(run, Error))
                return errors;

            var items = new HashSet<ItemDefAsset>(run.items);
            var stats = new HashSet<StatDefAsset>(run.stats);
            ValidateIds(run, Error);

            foreach (var item in run.items)
                ValidateItem(run, item, items, stats, Error);
            if (run.scrap.kind != ItemKind.Scrap)
                Error($"scrap '{run.scrap.id}' phải có Kind Scrap.");

            foreach (var item in run.shopPool)
            {
                Registered(item, items, "shopPool", Error);
                if (item.HasRecipe || item.tier < 1 || item.tier > run.shopMaxTier || item.kind == ItemKind.Scrap)
                    Error($"I24: Chợ không được bán '{item.id}' (tier {item.tier}, có công thức: {item.HasRecipe}).");
            }

            foreach (var item in run.starterKit)
                Registered(item, items, "starterKit", Error);
            if (run.starterKit.Count > run.bagCapacity)
                Error($"Bộ khởi đầu {run.starterKit.Count} vật phẩm vượt sức chứa Túi {run.bagCapacity}.");

            ValidateBattle(run, stats, Error);
            ValidateRewards(run, items, Error);
            ValidateModifiers(run, Error);
            ValidateRecipeCollisions(run, Error);
            ValidateReachability(run, Error);
            return errors;
        }

        private static void ValidateConfig(RunDefAsset r, System.Action<string> error)
        {
            if (string.IsNullOrEmpty(r.id))
                error("RunDef thiếu id.");
            if (r.gridWidth < 1 || r.gridHeight < 1 || r.gridWidth * r.gridHeight < 4)
                error("I8: lưới phải có ít nhất 4 ô.");
            if (r.seasonsPerRun < 1 || r.brewsPerSeason < 1 || r.startReputation < 1)
                error("Số Mùa, số lượt nung, Uy tín khởi đầu phải ≥ 1.");
            if (r.startGold < 0 || r.bagCapacity < 1 || r.maxBagCapacity < r.bagCapacity || r.catalystSlots < 0)
                error("Vàng / sức chứa Túi / số Xúc tác ngoài giới hạn.");
            if (r.magicCapPercent < 0 || r.magicCapPercent > 100 || r.magicPercentPerTier < 0)
                error("Tham số ma thuật ngoài giới hạn.");
            if (r.shopMaxTier < 1 || r.shopTierWeights == null || r.shopTierWeights.Count != r.shopMaxTier)
                error("shopTierWeights phải có đúng shopMaxTier phần tử.");
            if (r.shopOfferCount < 1 || r.rerollBaseCost < 0 || r.rerollCostStep < 0 || r.sellPercent < 0 || r.scrapSellPrice < 0)
                error("Tham số Chợ ngoài giới hạn.");
            if (r.priceFactorMinPercent < 0 || r.priceFactorMaxPercent < r.priceFactorMinPercent)
                error("I20: biên độ giá Mùa sai.");
            if (r.modifierChancePercent < 0 || r.modifierChancePercent > 100 || r.modifierPercentMin < 0
                || r.modifierPercentMax < r.modifierPercentMin)
                error("I20: biên độ modifier sai.");
            if (r.qualityWeight < 0)
                error("qualityWeight phải ≥ 0.");
        }

        /// <summary>Phần tử null trong danh sách làm converter crash; báo ngay và dừng kiểm phần còn lại.</summary>
        private static bool HasNull(RunDefAsset r, System.Action<string> error)
        {
            var found = false;
            void Check<T>(List<T> list, string name) where T : UnityEngine.Object
            {
                if (list == null || list.Exists(x => x == null))
                {
                    error($"{name} có phần tử trống.");
                    found = true;
                }
            }

            Check(r.stats, "stats");
            Check(r.items, "items");
            Check(r.shopPool, "shopPool");
            Check(r.starterKit, "starterKit");
            Check(r.opponents, "opponents");
            Check(r.equipment, "equipment");
            Check(r.rewards, "rewards");
            Check(r.modifiers, "modifiers");
            if (r.scrap == null)
            {
                error("Thiếu scrap.");
                found = true;
            }

            if (r.starThresholds == null)
            {
                error("Thiếu starThresholds.");
                found = true;
            }

            return found;
        }

        private static void ValidateIds(RunDefAsset r, System.Action<string> error)
        {
            void Unique(IEnumerable<string> ids, string name)
            {
                var seen = new HashSet<string>();
                foreach (var id in ids)
                    if (string.IsNullOrEmpty(id))
                        error($"{name}: có id rỗng.");
                    else if (!seen.Add(id))
                        error($"{name}: trùng id '{id}'.");
            }

            var itemIds = r.items.ConvertAll(i => i.id);
            itemIds.Add(r.scrap.id);
            Unique(itemIds, "items");
            Unique(r.stats.ConvertAll(s => s.id), "stats");
            Unique(r.opponents.ConvertAll(o => o.id), "opponents");
            Unique(r.equipment.ConvertAll(e => e.id), "equipment");
            Unique(r.rewards.ConvertAll(x => x.id), "rewards");
            Unique(r.modifiers.ConvertAll(m => m.id), "modifiers");

            var statIds = r.stats.ConvertAll(s => s.id);
            if (!statIds.Contains(StatIds.Quality) || !statIds.Contains(StatIds.Magic))
                error($"stats phải có '{StatIds.Quality}' và '{StatIds.Magic}'.");
        }

        private static void ValidateItem(RunDefAsset run, ItemDefAsset item, HashSet<ItemDefAsset> items,
            HashSet<StatDefAsset> stats, System.Action<string> error)
        {
            var id = item.id;
            if (item.kind == ItemKind.Scrap)
                error($"'{id}': phế phẩm chỉ khai báo ở scrap.");
            if (item.tier < 1 || item.tier > MaxTier)
                error($"'{id}': tier {item.tier} ngoài 1–{MaxTier}.");
            if (item.kind == ItemKind.Reagent)
                error($"'{id}': Thuốc thử chưa có trong MVP M4.");

            if (item.kind == ItemKind.Additive)
            {
                if (item.magicChance < 1 || item.magicChance > MaxMagicChancePerAdditive)
                    error($"'{id}': MagicChance phụ gia phải trong 1–{MaxMagicChancePerAdditive}%.");
                if (item.effects.Count > 0)
                    error($"'{id}': phụ gia chỉ được dùng AddMagicChance (magicChance), không có Effect.");
            }
            else if (item.magicChance != 0)
            {
                error($"'{id}': chỉ phụ gia có magicChance.");
            }

            if (item.kind == ItemKind.Lens && item.revealLines < 1)
                error($"'{id}': Kính soi cần revealLines ≥ 1.");
            if (item.kind != ItemKind.Catalyst && item.effects.Count > 0)
                error($"'{id}': chỉ Xúc tác có Effect.");

            if (item.HasRecipe)
                ValidateRecipe(run, item, items, stats, error);
            foreach (var e in item.effects)
                ValidateEffect(item, e, items, stats, error);
        }

        private static void ValidateRecipe(RunDefAsset run, ItemDefAsset item, HashSet<ItemDefAsset> items,
            HashSet<StatDefAsset> stats, System.Action<string> error)
        {
            var id = item.id;
            var r = item.recipe;
            if (item.kind != ItemKind.Mineral && item.kind != ItemKind.Additive)
                error($"'{id}': chỉ khoáng / phụ gia có công thức.");
            if (r.pattern.Count > run.gridWidth * run.gridHeight)
                error($"'{id}': Pattern dài {r.pattern.Count} > {run.gridWidth * run.gridHeight} ô.");
            if (r.pattern[0].item == null || r.pattern[r.pattern.Count - 1].item == null)
                error($"'{id}': Pattern phải bắt đầu và kết thúc bằng nguyên liệu.");
            foreach (var slot in r.pattern)
            {
                if (slot.item == null)
                {
                    if (slot.requiresMagic)
                        error($"'{id}': RequiresMagic trên Gap.");
                    continue;
                }

                Registered(slot.item, items, $"'{id}'.pattern", error);
                if (slot.item.kind != ItemKind.Mineral && slot.item.kind != ItemKind.Additive)
                    error($"'{id}': nguyên liệu '{slot.item.id}' không cho vào lò được.");
                if (slot.item == item)
                    error($"'{id}': công thức dùng chính nó.");
            }

            foreach (var a in r.allowedAdditives)
                if (a == null || a.kind != ItemKind.Additive)
                    error($"'{id}': allowedAdditives chỉ chứa phụ gia.");
                else
                    Registered(a, items, $"'{id}'.allowedAdditives", error);

            if (r.baseEssence < 0)
                error($"'{id}': baseEssence âm.");
            foreach (var s in r.stats)
                if (s.stat == null || !stats.Contains(s.stat) || s.stat.id == StatIds.Quality || s.stat.id == StatIds.Magic
                    || s.chancePercent < 0 || s.chancePercent > 100 || s.value < 0)
                    error($"'{id}': dòng thuộc tính khởi đầu sai.");
        }

        private static void ValidateEffect(ItemDefAsset item, EffectData e, HashSet<ItemDefAsset> items,
            HashSet<StatDefAsset> stats, System.Action<string> error)
        {
            var id = item.id;
            var productAction = e.action == EffectAction.AddEssence || e.action == EffectAction.AddPotency
                || e.action == EffectAction.MulPotency || e.action == EffectAction.AddStat;
            if (productAction && e.trigger != EffectTrigger.OnRecipeMatch)
                error($"'{id}': {e.action} chỉ dùng với OnRecipeMatch.");

            switch (e.action)
            {
                case EffectAction.AddEssence:
                case EffectAction.AddPotency:
                    if (e.value < 0)
                        error($"'{id}': {e.action} âm.");
                    break;
                case EffectAction.MulPotency:
                    if (e.value <= 0 || e.value > MaxMulPotency)
                        error($"'{id}': MulPotency phải trong (0, ×{MaxMulPotency}].");
                    break;
                case EffectAction.AddStat:
                    if (e.stat == null || !stats.Contains(e.stat) || e.stat.id == StatIds.Quality || e.stat.id == StatIds.Magic)
                        error($"'{id}': AddStat cần dòng thuộc tính khác Quality / Magic.");
                    else if (e.value < 0 || e.value > e.stat.maxEffectAdd)
                        error($"'{id}': AddStat {e.value} vượt giới hạn {e.stat.maxEffectAdd} của '{e.stat.id}'.");
                    break;
                case EffectAction.GainGold:
                    if (e.value < 1 || e.value > MaxGoldPerEffect || e.value != System.Math.Floor(e.value))
                        error($"'{id}': GainGold phải là số nguyên 1–{MaxGoldPerEffect}.");
                    break;
                case EffectAction.CreateIngredient:
                    if (e.item == null || (e.item.kind != ItemKind.Mineral && e.item.kind != ItemKind.Additive))
                        error($"'{id}': CreateIngredient cần một khoáng / phụ gia.");
                    else
                        Registered(e.item, items, $"'{id}'.CreateIngredient", error);
                    break;
            }

            foreach (var c in e.conditions)
            {
                if (e.trigger == EffectTrigger.OnShopEnter && c.kind != EffectConditionKind.Chance)
                    error($"'{id}': OnShopEnter chỉ dùng điều kiện Chance.");
                switch (c.kind)
                {
                    case EffectConditionKind.IngredientIs:
                    case EffectConditionKind.RecipeIs:
                        if (c.item == null || (c.kind == EffectConditionKind.RecipeIs && !c.item.HasRecipe))
                            error($"'{id}': {c.kind} cần một vật phẩm{(c.kind == EffectConditionKind.RecipeIs ? " có công thức" : "")}.");
                        else
                            Registered(c.item, items, $"'{id}'.{c.kind}", error);
                        break;
                    case EffectConditionKind.AdditiveCount:
                        if (c.amount < 1)
                            error($"'{id}': AdditiveCount cần amount ≥ 1.");
                        break;
                    case EffectConditionKind.Chance:
                        if (c.amount < 1 || c.amount > 99)
                            error($"'{id}': Chance phải trong 1–99%.");
                        break;
                }
            }
        }

        private static void ValidateBattle(RunDefAsset r, HashSet<StatDefAsset> stats, System.Action<string> error)
        {
            if (r.opponents.Count != r.seasonsPerRun)
                error($"Cần đúng {r.seasonsPerRun} đối thủ, có {r.opponents.Count}.");
            foreach (var o in r.opponents)
                if (o.power < 0)
                    error($"Đối thủ '{o.id}': Power âm.");

            if (r.equipment.Count == 0)
                error("Không có trang bị để rèn.");
            var totalWeight = 0;
            foreach (var e in r.equipment)
            {
                totalWeight += System.Math.Max(0, e.poolWeight);
                if (e.poolWeight < 0)
                    error($"Trang bị '{e.id}': poolWeight âm.");
                foreach (var w in e.statWeights)
                    if (w.stat == null || !stats.Contains(w.stat) || w.weight < 0)
                        error($"Trang bị '{e.id}': trọng số thuộc tính sai.");
            }

            if (r.equipment.Count > 0 && totalWeight == 0)
                error("Tổng trọng số bể Rèn = 0.");
        }

        private static void ValidateRewards(RunDefAsset r, HashSet<ItemDefAsset> items, System.Action<string> error)
        {
            foreach (var x in r.rewards)
            {
                if (x.minSeason > x.maxSeason)
                    error($"Chiến lợi phẩm '{x.id}': minSeason > maxSeason.");
                switch (x.kind)
                {
                    case RewardKind.BagExpand:
                        if (x.amount < 1)
                            error($"Chiến lợi phẩm '{x.id}': Nới Túi cần amount ≥ 1.");
                        break;
                    case RewardKind.RareMineral:
                        if (x.item == null || x.item.kind != ItemKind.Mineral || x.item.tier <= r.shopMaxTier)
                            error($"Chiến lợi phẩm '{x.id}': Khoáng hiếm phải là khoáng tier > {r.shopMaxTier}.");
                        else
                            Registered(x.item, items, $"reward '{x.id}'", error);
                        break;
                    case RewardKind.Catalyst:
                        if (x.item == null || x.item.kind != ItemKind.Catalyst)
                            error($"Chiến lợi phẩm '{x.id}': cần một Xúc tác.");
                        else
                            Registered(x.item, items, $"reward '{x.id}'", error);
                        break;
                }
            }
        }

        private static void ValidateModifiers(RunDefAsset r, System.Action<string> error)
        {
            foreach (var m in r.modifiers)
            foreach (var s in m.scales)
            {
                if (s.percent < r.modifierPercentMin || s.percent > r.modifierPercentMax)
                    error($"I20: modifier '{m.id}' {s.param} {s.percent}% ngoài [{r.modifierPercentMin}, {r.modifierPercentMax}].");
                if (s.param == ScaledParam.EquipmentWeight && (s.target == null || !r.equipment.Contains(s.target)))
                    error($"Modifier '{m.id}': EquipmentWeight cần trang bị của run.");
            }
        }

        /// <summary>
        /// I2: hai công thức cùng khớp một chuỗi khi cùng độ dài (slot cuối luôn là nguyên liệu) và từng slot tương thích:
        /// cùng nguyên liệu, hoặc nguyên liệu bên này là phụ gia được phép ở Gap bên kia, hoặc cả hai là Gap.
        /// </summary>
        private static void ValidateRecipeCollisions(RunDefAsset r, System.Action<string> error)
        {
            var recipes = r.items.FindAll(i => i.HasRecipe);
            for (var i = 0; i < recipes.Count; i++)
                for (var j = i + 1; j < recipes.Count; j++)
                    if (Collide(recipes[i].recipe, recipes[j].recipe))
                        error($"I2: công thức '{recipes[i].id}' và '{recipes[j].id}' cùng khớp một chuỗi nạp.");
        }

        private static bool Collide(RecipeData p, RecipeData q)
        {
            if (p.pattern.Count != q.pattern.Count)
                return false;
            for (var k = 0; k < p.pattern.Count; k++)
            {
                var a = p.pattern[k].item;
                var b = q.pattern[k].item;
                var ok = a == null && b == null
                         || a != null && b != null && a == b
                         || a == null && b.kind == ItemKind.Additive && p.allowedAdditives.Contains(b)
                         || b == null && a.kind == ItemKind.Additive && q.allowedAdditives.Contains(a);
                if (!ok)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Mọi khoáng có công thức (và khoáng tier 10) phải nung được từ nguồn khai báo: Chợ, bộ khởi đầu, chiến lợi
        /// phẩm, CreateIngredient. Slot RequiresMagic cần nguyên liệu có thể Kích hoạt ma thuật.
        /// </summary>
        private static void ValidateReachability(RunDefAsset r, System.Action<string> error)
        {
            var reachable = new HashSet<ItemDefAsset>(r.shopPool);
            reachable.UnionWith(r.starterKit);
            foreach (var x in r.rewards)
                if (x.item != null)
                    reachable.Add(x.item);
            foreach (var item in r.items)
                foreach (var e in item.effects)
                    if (e.action == EffectAction.CreateIngredient && e.item != null && reachable.Contains(item))
                        reachable.Add(e.item);

            var magic = new HashSet<ItemDefAsset>();
            for (var changed = true; changed;)
            {
                changed = false;
                foreach (var item in r.items)
                {
                    if (!item.HasRecipe)
                        continue;
                    var slots = item.recipe.pattern;
                    if (!reachable.Contains(item)
                        && slots.TrueForAll(s => s.item == null || reachable.Contains(s.item) && (!s.requiresMagic || magic.Contains(s.item))))
                        changed |= reachable.Add(item);
                    // Ma thuật đến từ phụ gia trong khoảng trống, nguyên liệu là phụ gia, hoặc truyền thừa.
                    if (reachable.Contains(item) && !magic.Contains(item)
                        && (item.recipe.allowedAdditives.Exists(a => a != null && a.magicChance > 0 && reachable.Contains(a))
                            || slots.Exists(s => s.item != null && (magic.Contains(s.item) || s.item.kind == ItemKind.Additive && s.item.magicChance > 0))))
                        changed |= magic.Add(item);
                }
            }

            var hasSupreme = false;
            foreach (var item in r.items)
            {
                if (item.kind == ItemKind.Mineral && item.tier == MaxTier)
                    hasSupreme = true;
                if (item.HasRecipe && !reachable.Contains(item))
                    error($"'{item.id}': không nung được từ nguyên liệu Chợ / bộ khởi đầu / chiến lợi phẩm.");
                else if (item.kind == ItemKind.Mineral && item.tier == MaxTier && !reachable.Contains(item))
                    error($"'{item.id}': khoáng tier {MaxTier} không có đường tới.");
            }

            if (!hasSupreme)
                error($"Thiếu khoáng tier {MaxTier} (Khoáng thạch Ma thuật Tối thượng).");
        }

        private static void Registered(ItemDefAsset item, HashSet<ItemDefAsset> items, string where, System.Action<string> error)
        {
            if (!items.Contains(item))
                error($"{where}: '{item.id}' không có trong items của run.");
        }
    }
}
