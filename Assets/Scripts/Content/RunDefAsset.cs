using System.Collections.Generic;
using UnityEngine;

namespace Alchemy.Content
{
    /// <summary>Config + toàn bộ content của một run. Số liệu cân bằng là tạm (CLAUDE.md mục 10).</summary>
    [CreateAssetMenu(menuName = "Crucible/Run", fileName = "Run")]
    public sealed class RunDefAsset : ScriptableObject
    {
        public string id;

        [Header("Lò")]
        public int gridWidth = 3;
        public int gridHeight = 3;
        public int magicCapPercent = 90;
        public int magicPercentPerTier = 5;

        [Header("Run")]
        public int seasonsPerRun = 8;
        public int brewsPerSeason = 3;
        public int startReputation = 3;
        public int startGold;
        public int bagCapacity = 14;
        public int maxBagCapacity = 24;
        public int catalystSlots = 5;

        [Header("Chợ")]
        public int shopMaxTier = 3;
        [Tooltip("Trọng số tier 1..ShopMaxTier.")]
        public List<int> shopTierWeights = new List<int> { 70, 25, 5 };
        public int shopOfferCount = 6;
        public int rerollBaseCost = 2;
        public int rerollCostStep = 1;
        public int priceFactorMinPercent = 70;
        public int priceFactorMaxPercent = 130;
        public int sellPercent = 60;
        public int scrapSellPrice = 1;

        [Header("Modifier Mùa")]
        public int modifierChancePercent = 60;
        public int modifierPercentMin = 50;
        public int modifierPercentMax = 300;

        [Header("Trận")]
        public double qualityWeight = 1;
        [Tooltip("Power đối thủ ≥ ngưỡng thứ i → thêm 1 sao.")]
        public List<double> starThresholds = new List<double>();

        [Header("Danh vọng")]
        public int renownPerWin = 10;
        public int renownPerTier = 5;
        public int renownRunWin = 30;

        [Header("Content")]
        public List<StatDefAsset> stats = new List<StatDefAsset>();
        [Tooltip("Mọi vật phẩm của run (trừ phế phẩm), thứ tự cố định.")]
        public List<ItemDefAsset> items = new List<ItemDefAsset>();
        public ItemDefAsset scrap;
        public List<ItemDefAsset> shopPool = new List<ItemDefAsset>();
        [Tooltip("Phần tử đầu là mặc định, luôn mở.")]
        public List<StarterKitDefAsset> starterKits = new List<StarterKitDefAsset>();
        [Tooltip("Từ dễ tới khó; phần tử đầu luôn mở.")]
        public List<DifficultyDefAsset> difficulties = new List<DifficultyDefAsset>();
        public List<EquipmentDefAsset> equipment = new List<EquipmentDefAsset>();
        public List<RewardDefAsset> rewards = new List<RewardDefAsset>();
        public List<SeasonModifierDefAsset> modifiers = new List<SeasonModifierDefAsset>();

        /// <summary>Tên hiển thị của dòng thuộc tính; id nếu không có.</summary>
        public string StatName(string statId)
        {
            foreach (var s in stats)
                if (s != null && s.id == statId)
                    return s.displayName;
            return statId;
        }
    }
}
