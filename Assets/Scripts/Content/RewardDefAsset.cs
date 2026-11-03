using Alchemy.Core;
using UnityEngine;

namespace Alchemy.Content
{
    [CreateAssetMenu(menuName = "Crucible/Reward", fileName = "Reward")]
    public sealed class RewardDefAsset : ScriptableObject
    {
        public string id;
        public string displayName;
        public RewardKind kind;

        [Tooltip("RareMineral, Catalyst.")]
        public ItemDefAsset item;

        [Tooltip("BagExpand: số chỗ nới.")]
        [Min(0)] public int amount;

        [Min(1)] public int minSeason = 1;
        [Min(1)] public int maxSeason = 99;
    }
}
