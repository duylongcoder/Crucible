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

    /// <summary>Bậc khó: danh sách đối thủ riêng theo Mùa + hệ số Danh vọng. Bậc i + 1 mở khi thắng run ở bậc i.</summary>
    public sealed class DifficultyDef
    {
        public string Id { get; }
        public string Name { get; }

        /// <summary>Đối thủ Mùa i ở phần tử i − 1.</summary>
        public IReadOnlyList<OpponentDef> Opponents { get; }

        /// <summary>Danh vọng cuối run nhân với hệ số này (%).</summary>
        public int RenownPercent { get; }

        public DifficultyDef(string id, string name, IReadOnlyList<OpponentDef> opponents, int renownPercent = 100)
        {
            Id = id;
            Name = name;
            Opponents = opponents;
            RenownPercent = renownPercent;
        }
    }

    public sealed class StarterKitDef
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public IReadOnlyList<ItemDef> Items { get; }

        /// <summary>Danh vọng tích lũy cần để chọn được bộ này.</summary>
        public int UnlockRenown { get; }

        public StarterKitDef(string id, string name, IReadOnlyList<ItemDef> items, string description = "", int unlockRenown = 0)
        {
            Id = id;
            Name = name;
            Items = items;
            Description = description;
            UnlockRenown = unlockRenown;
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
        Catalyst,
    }

    public sealed class RewardDef
    {
        public string Id { get; }
        public string Name { get; }
        public RewardKind Kind { get; }

        /// <summary>RareMineral, Catalyst: vật phẩm nhận được.</summary>
        public ItemDef Item { get; }

        /// <summary>BagExpand: số chỗ nới thêm.</summary>
        public int Amount { get; }

        public int MinSeason { get; }
        public int MaxSeason { get; }

        /// <summary>Danh vọng tích lũy cần để chiến lợi phẩm này vào bể.</summary>
        public int UnlockRenown { get; }

        public RewardDef(string id, string name, RewardKind kind, ItemDef item = null, int amount = 0,
            int minSeason = 1, int maxSeason = int.MaxValue, int unlockRenown = 0)
        {
            UnlockRenown = unlockRenown;
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

        /// <summary>Số Xúc tác tối đa trong Túi; mua / nhận thêm bị chặn.</summary>
        public int CatalystSlots { get; set; }

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

        /// <summary>Phần tử đầu là mặc định (luôn mở).</summary>
        public IReadOnlyList<StarterKitDef> StarterKits { get; set; }

        /// <summary>Từ dễ tới khó; phần tử đầu luôn mở.</summary>
        public IReadOnlyList<DifficultyDef> Difficulties { get; set; }

        /// <summary>Danh vọng = (Thắng × RenownPerWin + tier cao nhất đã nung × RenownPerTier + thắng run ? RenownRunWin) × hệ số bậc khó.</summary>
        public int RenownPerWin { get; set; }
        public int RenownPerTier { get; set; }
        public int RenownRunWin { get; set; }
        public IReadOnlyList<EquipmentDef> Equipment { get; set; }
        public IReadOnlyList<RewardDef> Rewards { get; set; }
        public IReadOnlyList<SeasonModifierDef> Modifiers { get; set; }

        public const int RewardChoiceCount = 3;

        /// <summary>Item trong <see cref="Items"/> hoặc <see cref="Scrap"/> theo id; null nếu không có.</summary>
        public ItemDef FindItem(string id)
        {
            if (Scrap != null && Scrap.Id == id)
                return Scrap;
            foreach (var item in Items)
                if (item.Id == id)
                    return item;
            return null;
        }
    }
}
