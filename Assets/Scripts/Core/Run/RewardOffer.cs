using System.Collections.Generic;

namespace Alchemy.Core
{
    /// <summary>Rút tối đa 3 chiến lợi phẩm khác nhau, lọc theo điều kiện xuất hiện (stream Reward, I22).</summary>
    internal static class RewardOffer
    {
        public static void Roll(RunState state)
        {
            var candidates = new List<RewardDef>();
            foreach (var r in state.Def.Rewards)
                if (IsAvailable(state, r))
                    candidates.Add(r);

            var rng = state.Rng[RngStream.Reward];
            state.RewardList.Clear();
            for (var k = 0; k < RunDef.RewardChoiceCount && k < candidates.Count; k++)
            {
                var j = k + rng.NextInt(candidates.Count - k);
                (candidates[k], candidates[j]) = (candidates[j], candidates[k]);
                state.RewardList.Add(candidates[k]);
            }
        }

        public static bool IsAvailable(RunState state, RewardDef reward)
        {
            if (state.Season < reward.MinSeason || state.Season > reward.MaxSeason || reward.UnlockRenown > state.Renown)
                return false;
            switch (reward.Kind)
            {
                case RewardKind.BagExpand:
                    return reward.Amount > 0 && state.BagCapacity < state.Def.MaxBagCapacity;
                case RewardKind.RareMineral:
                    // CreateIngredient bị chặn khi Túi đầy.
                    return reward.Item != null && state.BagItems.Count < state.BagCapacity;
                case RewardKind.Catalyst:
                    return reward.Item != null && state.BagItems.Count < state.BagCapacity
                        && state.CatalystCount < state.Def.CatalystSlots;
                default:
                    return false;
            }
        }

        public static void Apply(RunState state, RewardDef reward)
        {
            switch (reward.Kind)
            {
                case RewardKind.BagExpand:
                    state.BagCapacity = System.Math.Min(state.Def.MaxBagCapacity, state.BagCapacity + reward.Amount);
                    break;
                case RewardKind.RareMineral:
                case RewardKind.Catalyst:
                    state.BagItems.Add(new Product(reward.Item));
                    state.MarkAcquired(reward.Item);
                    break;
            }
        }
    }
}
