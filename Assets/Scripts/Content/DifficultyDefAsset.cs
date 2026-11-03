using System.Collections.Generic;
using UnityEngine;

namespace Alchemy.Content
{
    [CreateAssetMenu(menuName = "Crucible/Difficulty", fileName = "Difficulty")]
    public sealed class DifficultyDefAsset : ScriptableObject
    {
        public string id;
        public string displayName;

        [Tooltip("Đối thủ Mùa i ở phần tử i − 1.")]
        public List<OpponentDefAsset> opponents = new List<OpponentDefAsset>();

        [Tooltip("Danh vọng cuối run nhân hệ số này (%).")]
        [Min(0)] public int renownPercent = 100;
    }
}
