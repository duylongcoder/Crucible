using System.Collections.Generic;
using Alchemy.Core;

namespace Alchemy.Content
{
    /// <summary>ScriptableObject → data immutable của Core. Giả định content đã qua <see cref="ContentValidator"/>.</summary>
    public static class ContentConverter
    {
        public static RunDef ToRunDef(RunDefAsset a)
        {
            var items = new Dictionary<ItemDefAsset, ItemDef>();
            ItemDef Item(ItemDefAsset x)
            {
                if (!items.TryGetValue(x, out var def))
                    items[x] = def = Convert(x);
                return def;
            }

            List<ItemDef> Items(List<ItemDefAsset> list) => list.ConvertAll(Item);

            var equipment = a.equipment.ConvertAll(e => new EquipmentDef(e.id, e.displayName, e.poolWeight,
                e.statWeights.ConvertAll(w => new StatWeight(w.stat.id, w.weight))));

            return new RunDef
            {
                Id = a.id,
                Brew = new BrewRules(a.gridWidth, a.gridHeight, a.magicCapPercent, a.magicPercentPerTier),
                SeasonsPerRun = a.seasonsPerRun,
                BrewsPerSeason = a.brewsPerSeason,
                StartReputation = a.startReputation,
                StartGold = a.startGold,
                BagCapacity = a.bagCapacity,
                MaxBagCapacity = a.maxBagCapacity,
                CatalystSlots = a.catalystSlots,
                ShopMaxTier = a.shopMaxTier,
                ShopTierWeights = a.shopTierWeights.ToArray(),
                ShopOfferCount = a.shopOfferCount,
                RerollBaseCost = a.rerollBaseCost,
                RerollCostStep = a.rerollCostStep,
                PriceFactorMinPercent = a.priceFactorMinPercent,
                PriceFactorMaxPercent = a.priceFactorMaxPercent,
                SellPercent = a.sellPercent,
                ScrapSellPrice = a.scrapSellPrice,
                ModifierChancePercent = a.modifierChancePercent,
                ModifierPercentMin = a.modifierPercentMin,
                ModifierPercentMax = a.modifierPercentMax,
                QualityWeight = a.qualityWeight,
                StarThresholds = a.starThresholds.ToArray(),
                Items = Items(a.items),
                Scrap = Item(a.scrap),
                ShopPool = Items(a.shopPool),
                StarterKits = a.starterKits.ConvertAll(k => new StarterKitDef(k.id, k.displayName, Items(k.items), k.description, k.unlockRenown)),
                Difficulties = a.difficulties.ConvertAll(d => new DifficultyDef(d.id, d.displayName,
                    d.opponents.ConvertAll(o => new OpponentDef(o.id, o.displayName, o.battleKind, o.power)), d.renownPercent)),
                RenownPerWin = a.renownPerWin,
                RenownPerTier = a.renownPerTier,
                RenownRunWin = a.renownRunWin,
                Equipment = equipment,
                Rewards = a.rewards.ConvertAll(r => new RewardDef(r.id, r.displayName, r.kind,
                    r.item == null ? null : Item(r.item), r.amount, r.minSeason, r.maxSeason, r.unlockRenown)),
                Modifiers = a.modifiers.ConvertAll(m => new SeasonModifierDef(m.id, m.displayName, m.description,
                    m.scales.ConvertAll(s => new ParamScale(s.param, s.percent, s.target == null ? null : s.target.id)),
                    m.freeRerolls)),
            };
        }

        private static ItemDef Convert(ItemDefAsset x)
        {
            Recipe recipe = null;
            if (x.HasRecipe)
            {
                var r = x.recipe;
                recipe = new Recipe(
                    r.pattern.ConvertAll(s => s.item == null ? RecipeSlot.Gap : new RecipeSlot(s.item.id, s.requiresMagic)),
                    r.baseEssence,
                    r.allowedAdditives.ConvertAll(i => i.id),
                    r.stats.ConvertAll(s => new RecipeStat(s.stat.id, s.chancePercent, s.value)));
            }

            var effects = x.effects.ConvertAll(e => new EffectDef(e.trigger, e.action, e.value,
                e.action == EffectAction.AddStat ? e.stat.id : e.action == EffectAction.CreateIngredient ? e.item.id : null,
                e.conditions.ConvertAll(c => new EffectCondition(c.kind, c.item == null ? null : c.item.id, c.amount))));

            return new ItemDef(x.id, x.kind, x.tier, recipe, x.magicChance, x.displayName, x.marketPrice, x.revealLines,
                x.description, effects, x.unlockRenown);
        }
    }
}
