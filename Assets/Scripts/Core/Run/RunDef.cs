using System.Collections.Generic;

namespace Alchemy.Core
{
    public enum BattleKind
    {
        Tournament,
        War,
        GrandWar,
    }

    public sealed class OpponentDef
    {
        public string Id { get; }
        public string Name { get; }
        public BattleKind Kind { get; }
        internal double Power { get; }

        public OpponentDef(string id, string name, BattleKind kind, double power)
        {
            Id = id;
            Name = name;
            Kind = kind;
            Power = power;
        }
    }

    public sealed class StatWeight
    {
        internal string StatId { get; }
        internal double Weight { get; }

        public StatWeight(string statId, double weight)
        {
            StatId = statId;
            Weight = weight;
        }
    }

    public sealed class EquipmentDef
    {
        public string Id { get; }
        public string Name { get; }

        /// <summary>Trọng số rút trong bể Rèn (stream Forge).</summary>
        internal int PoolWeight { get; }
        internal IReadOnlyList<StatWeight> StatWeights { get; }

        public EquipmentDef(string id, string name, int poolWeight, IReadOnlyList<StatWeight> statWeights)
        {
            Id = id;
            Name = name;
            PoolWeight = poolWeight;
            StatWeights = statWeights;
        }
    }

    public enum RewardKind
    {
        BagExpand,
        RareMineral,
    }

    public sealed class RewardDef
    {
        public string Id { get; }
        public string Name { get; }
        public RewardKind Kind { get; }

        /// <summary>RareMineral: khoáng nhận được.</summary>
        public ItemDef Item { get; }

        /// <summary>BagExpand: số chỗ nới thêm.</summary>
        public int Amount { get; }

        public int MinSeason { get; }
        public int MaxSeason { get; }

        public RewardDef(string id, string name, RewardKind kind, ItemDef item = null, int amount = 0,
            int minSeason = 1, int maxSeason = int.MaxValue)
        {
            Id = id;
            Name = name;
            Kind = kind;
            Item = item;
            Amount = amount;
            MinSeason = minSeason;
            MaxSeason = maxSeason;
        }
    }

    /// <summary>Danh sách đóng các tham số modifier được phép nhân (ScaleParam).</summary>
    public enum ScaledParam
    {
        BuyPrice,
        SellPrice,
        LensPrice,
        RerollPrice,
        OpponentPower,
        EquipmentWeight,
    }

    public sealed class ParamScale
    {
        public ScaledParam Param { get; }

        /// <summary>Số nguyên phần trăm, trong [RunDef.ModifierPercentMin, RunDef.ModifierPercentMax] (I20).</summary>
        public int Percent { get; }

        /// <summary>EquipmentWeight: id trang bị bị nhân trọng số.</summary>
        public string TargetId { get; }

        public ParamScale(ScaledParam param, int percent, string targetId = null)
        {
            Param = param;
            Percent = percent;
            TargetId = targetId;
        }
    }

    public sealed class SeasonModifierDef
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public IReadOnlyList<ParamScale> Scales { get; }

        /// <summary>OffsetParam: số lần reroll miễn phí mỗi lượt Chợ (0–2).</summary>
        public int FreeRerolls { get; }

        public SeasonModifierDef(string id, string name, string description, IReadOnlyList<ParamScale> scales = null,
            int freeRerolls = 0)
        {
            Id = id;
            Name = name;
            Description = description;
            Scales = scales ?? System.Array.Empty<ParamScale>();
            FreeRerolls = freeRerolls;
        }
    }

    /// <summary>
    /// Toàn bộ data của một run. Content dựng bằng object initializer rồi coi như immutable.
    /// Số liệu cân bằng ẩn (Power, trọng số) là internal.
    /// </summary>
    public sealed class RunDef
    {
        public string Id { get; set; }
        public BrewRules Brew { get; set; }

        public int SeasonsPerRun { get; set; }
        public int BrewsPerSeason { get; set; }
        public int StartReputation { get; set; }
        public int StartGold { get; set; }
        public int BagCapacity { get; set; }
        public int MaxBagCapacity { get; set; }

        public int ShopMaxTier { get; set; }

        /// <summary>Trọng số tier 1..ShopMaxTier khi bày hàng, phần tử i ứng với tier i + 1.</summary>
        public IReadOnlyList<int> ShopTierWeights { get; set; }
        public int ShopOfferCount { get; set; }
        public int RerollBaseCost { get; set; }
        public int RerollCostStep { get; set; }

        public int PriceFactorMinPercent { get; set; }
        public int PriceFactorMaxPercent { get; set; }
        public int SellPercent { get; set; }
        public int ScrapSellPrice { get; set; }

        public int ModifierChancePercent { get; set; }
        public int ModifierPercentMin { get; set; }
        public int ModifierPercentMax { get; set; }

        internal double QualityWeight { get; set; }

        /// <summary>Power đối thủ ≥ ngưỡng thứ i → thêm 1 sao (★1 + số ngưỡng vượt qua).</summary>
        internal IReadOnlyList<double> StarThresholds { get; set; }

        /// <summary>Mọi item của run, theo thứ tự cố định (dùng cho hệ số giá).</summary>
        public IReadOnlyList<ItemDef> Items { get; set; }
        public ItemDef Scrap { get; set; }
        public IReadOnlyList<ItemDef> ShopPool { get; set; }
        public IReadOnlyList<ItemDef> StarterKit { get; set; }

        /// <summary>Đối thủ Mùa i ở phần tử i − 1.</summary>
        public IReadOnlyList<OpponentDef> Opponents { get; set; }
        public IReadOnlyList<EquipmentDef> Equipment { get; set; }
        public IReadOnlyList<RewardDef> Rewards { get; set; }
        public IReadOnlyList<SeasonModifierDef> Modifiers { get; set; }

        public const int RewardChoiceCount = 3;
    }
}
