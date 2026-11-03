using UnityEngine;

namespace Alchemy.Content
{
    /// <summary>Một dòng thuộc tính ẩn. Id "quality" và "magic" là dòng mặc định Core dùng.</summary>
    [CreateAssetMenu(menuName = "Crucible/Stat", fileName = "Stat")]
    public sealed class StatDefAsset : ScriptableObject
    {
        public string id;
        public string displayName;

        [Tooltip("Giới hạn khai báo cho mỗi Action AddStat.")]
        [Min(0)] public double maxEffectAdd;
    }
}
