using System.Collections.Generic;

namespace Alchemy.Core
{
    public static class Market
    {
        /// <summary>Chợ chỉ bán vật phẩm gốc (không có Recipe), tier ≤ ShopMaxTier (I24).</summary>
        public static bool IsSellable(RunDef def, ItemDef item) =>
            item.Recipe == null && item.Kind != ItemKind.Scrap && item.Tier >= 1 && item.Tier <= def.ShopMaxTier;

        public static int BuyPrice(RunState state, ItemDef item)
        {
            long price = (long)item.MarketPrice * Factor(state, item) / 100;
            price = price * SeasonModifiers.Scale(state, ScaledParam.BuyPrice) / 100;
            if (item.Kind == ItemKind.Lens)
                price = price * SeasonModifiers.Scale(state, ScaledParam.LensPrice) / 100;
            return (int)System.Math.Max(1, System.Math.Min(price, int.MaxValue));
        }

        /// <summary>Theo loại vật phẩm, không bao giờ theo thuộc tính.</summary>
        public static int SellPrice(RunState state, ItemDef item)
        {
            if (item.Kind == ItemKind.Scrap)
                return System.Math.Max(0, state.Def.ScrapSellPrice);

            long price = (long)item.MarketPrice * Factor(state, item) / 100;
            price = price * state.Def.SellPercent / 100;
            price = price * SeasonModifiers.Scale(state, ScaledParam.SellPrice) / 100;
            return (int)System.Math.Max(0, System.Math.Min(price, int.MaxValue));
        }

        public static int RerollCost(RunState state)
        {
            var paid = state.RerollsThisTurn - SeasonModifiers.FreeRerolls(state);
            if (paid < 0)
                return 0;
            long cost = state.Def.RerollBaseCost + (long)paid * state.Def.RerollCostStep;
            cost = cost * SeasonModifiers.Scale(state, ScaledParam.RerollPrice) / 100;
            return (int)System.Math.Max(0, System.Math.Min(cost, int.MaxValue));
        }

        internal static void RollPriceFactors(RunState state)
        {
            var def = state.Def;
            var rng = state.Rng[RngStream.Market];
            var span = def.PriceFactorMaxPercent - def.PriceFactorMinPercent + 1;
            state.PriceFactors = new int[def.Items.Count];
            for (var i = 0; i < def.Items.Count; i++)
                state.PriceFactors[i] = def.PriceFactorMinPercent + rng.NextInt(span);
        }

        /// <summary>Rút tier theo trọng số, rồi rút đều trong các item tier đó (stream Shop).</summary>
        internal static void RollOffers(RunState state)
        {
            var def = state.Def;
            var rng = state.Rng[RngStream.Shop];
            state.OfferList.Clear();

            var byTier = new List<ItemDef>[def.ShopMaxTier];
            var weights = new int[def.ShopMaxTier];
            for (var t = 0; t < def.ShopMaxTier; t++)
                byTier[t] = new List<ItemDef>();
            foreach (var item in def.ShopPool)
                if (IsSellable(def, item) && item.UnlockRenown <= state.Renown)
                    byTier[item.Tier - 1].Add(item);
            for (var t = 0; t < def.ShopMaxTier; t++)
                weights[t] = byTier[t].Count > 0 && t < def.ShopTierWeights.Count ? def.ShopTierWeights[t] : 0;

            for (var i = 0; i < def.ShopOfferCount; i++)
            {
                var tier = rng.PickWeighted(weights);
                if (tier < 0)
                    return;
                var pool = byTier[tier];
                var item = pool[rng.NextInt(pool.Count)];
                state.OfferList.Add(new ShopOffer(item, BuyPrice(state, item)));
            }
        }

        private static int Factor(RunState state, ItemDef item)
        {
            var i = IndexOf(state.Def.Items, item);
            return i < 0 || state.PriceFactors == null ? 100 : state.PriceFactors[i];
        }

        private static int IndexOf(IReadOnlyList<ItemDef> items, ItemDef item)
        {
            for (var i = 0; i < items.Count; i++)
                if (items[i] == item)
                    return i;
            return -1;
        }
    }
}
