using System;
using System.Collections.Generic;
using Alchemy.Core;
using UnityEngine;

namespace Alchemy.Content
{
    [CreateAssetMenu(menuName = "Crucible/Season Modifier", fileName = "Modifier")]
    public sealed class SeasonModifierDefAsset : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public List<ScaleData> scales = new List<ScaleData>();

        [Tooltip("OffsetParam: reroll miễn phí mỗi lượt Chợ (0–2).")]
        [Range(0, 2)] public int freeRerolls;
    }

    [Serializable]
    public sealed class ScaleData
    {
        public ScaledParam param;

        [Tooltip("Số nguyên phần trăm.")]
        public int percent = 100;

        [Tooltip("Chỉ EquipmentWeight.")]
        public EquipmentDefAsset target;
    }
}
