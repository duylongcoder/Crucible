using Alchemy.Core;
using UnityEngine;

namespace Alchemy.Content
{
    [CreateAssetMenu(menuName = "Crucible/Opponent", fileName = "Opponent")]
    public sealed class OpponentDefAsset : ScriptableObject
    {
        public string id;
        public string displayName;
        public BattleKind battleKind;

        [Tooltip("Ẩn với người chơi; UI chỉ hiện mức sao.")]
        [Min(0)] public double power;
    }
}
