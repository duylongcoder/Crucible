namespace Alchemy.Core
{
    /// <summary>Kiểm bất biến kiểm được trên một <see cref="RunState"/> (CLAUDE.md mục 6). Trả về mô tả vi phạm đầu tiên hoặc null.</summary>
    public static class InvariantChecker
    {
        public static string Check(RunState s)
        {
            var def = s.Def;

            if (s.Gold < 0)
                return $"I4: vàng âm ({s.Gold}).";
            foreach (var o in s.Offers)
            {
                if (o.Price < 1)
                    return $"I4: giá mua {o.Item.Id} = {o.Price} < 1.";
                if (!Market.IsSellable(def, o.Item))
                    return $"I24: Chợ bày {o.Item.Id} (tier {o.Item.Tier}, có Recipe: {o.Item.Recipe != null}).";
            }

            if (s.Grid.Width * s.Grid.Height != def.Brew.CellCount)
                return "I8: kích thước lưới khác config.";
            foreach (var cell in s.Grid.ToFeedSequence())
                if (cell != null && cell.Def.Kind == ItemKind.Scrap)
                    return "I13: phế phẩm trên lưới.";

            if (s.Bag.Count > s.BagCapacity || s.BagCapacity > def.MaxBagCapacity)
                return $"I10: Túi {s.Bag.Count}/{s.BagCapacity} (trần {def.MaxBagCapacity}).";

            if (s.CatalystCount > def.CatalystSlots)
                return $"I10: {s.CatalystCount} Xúc tác (tối đa {def.CatalystSlots}).";

            if (s.BrewsUsed < 0 || s.BrewsUsed > def.BrewsPerSeason)
                return $"I12: {s.BrewsUsed} lượt đã dùng.";
            if (s.Season < 1 || s.Season > def.SeasonsPerRun)
                return $"I12: Mùa {s.Season}.";

            foreach (var p in s.Bag)
            {
                var violation = CheckProduct(p);
                if (violation != null)
                    return violation;
            }

            if (s.Reputation < 0 || s.Reputation > def.StartReputation)
                return $"I19: Uy tín {s.Reputation}.";
            if (s.Reputation == 0 && s.Outcome != RunOutcome.Exiled)
                return "I19: Uy tín 0 nhưng chưa trục xuất.";

            if (s.PriceFactors != null)
                foreach (var f in s.PriceFactors)
                    if (f < def.PriceFactorMinPercent || f > def.PriceFactorMaxPercent)
                        return $"I20: hệ số giá {f}%.";
            foreach (var m in new[] { s.Modifier, s.NextModifier })
                if (m != null)
                    foreach (var scale in m.Scales)
                        if (scale.Percent < def.ModifierPercentMin || scale.Percent > def.ModifierPercentMax)
                            return $"I20: modifier {m.Id} {scale.Percent}%.";

            if (s.RewardChoices.Count > RunDef.RewardChoiceCount)
                return "I22: quá 3 chiến lợi phẩm.";
            if (s.Phase == RunPhase.Spoils && (s.LastBattle == null || !s.LastBattle.Won))
                return "I22: chiến lợi phẩm khi chưa thắng.";

            var b = s.LastBattle;
            if (b != null && (!IsFinite(b.Power) || b.Power < 0 || !IsFinite(b.OpponentPower) || b.OpponentPower < 0))
                return $"I23: Power {b.Power} / {b.OpponentPower}.";

            return null;
        }

        /// <summary>I3, I17.</summary>
        public static string CheckProduct(Product p)
        {
            if (!IsFinite(p.Essence) || !IsFinite(p.Potency) || p.Potency < 1 || p.Quality < 0)
                return $"I3: {p.Def.Id} Essence {p.Essence}, Potency {p.Potency}, Quality {p.Quality}.";
            foreach (var stat in p.Stats)
                if (!IsFinite(stat.Value))
                    return $"I3: {p.Def.Id}.{stat.StatId} = {stat.Value}.";
            if (p.RevealedCount > p.Stats.Count)
                return $"I17: {p.Def.Id} lộ {p.RevealedCount}/{p.Stats.Count} dòng.";
            return null;
        }

        private static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
    }
}
