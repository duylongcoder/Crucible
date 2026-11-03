using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alchemy.Content
{
    [CreateAssetMenu(menuName = "Crucible/Equipment", fileName = "Equipment")]
    public sealed class EquipmentDefAsset : ScriptableObject
    {
        public string id;
        public string displayName;

        [Tooltip("Trọng số rút trong bể Rèn.")]
        [Min(0)] public int poolWeight = 1;
        public List<StatWeightData> statWeights = new List<StatWeightData>();
    }

    [Serializable]
    public sealed class StatWeightData
    {
        public StatDefAsset stat;
        [Min(0)] public double weight;
    }
}
