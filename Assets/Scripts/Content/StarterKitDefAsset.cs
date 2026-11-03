using System.Collections.Generic;
using UnityEngine;

namespace Alchemy.Content
{
    [CreateAssetMenu(menuName = "Crucible/Starter Kit", fileName = "StarterKit")]
    public sealed class StarterKitDefAsset : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public List<ItemDefAsset> items = new List<ItemDefAsset>();

        [Tooltip("Danh vọng tích lũy cần để chọn bộ này.")]
        [Min(0)] public int unlockRenown;
    }
}
