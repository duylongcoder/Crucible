namespace Alchemy.Core
{
    /// <summary>Rèn: loại trang bị rút sau khi dâng (stream Forge); Power chỉ dùng cộng và nhân (I23).</summary>
    internal static class Forge
    {
        public static EquipmentDef Pick(RunState state)
        {
            var pool = state.Def.Equipment;
            var weights = new int[pool.Count];
            for (var i = 0; i < pool.Count; i++)
                weights[i] = pool[i].PoolWeight * SeasonModifiers.Scale(state, ScaledParam.EquipmentWeight, pool[i].Id) / 100;

            var index = state.Rng[RngStream.Forge].PickWeighted(weights);
            return pool[index < 0 ? 0 : index];
        }

        /// <summary>Power = Quality × QualityWeight + Σ(Stat_i × Weight_i).</summary>
        public static double Power(Product product, EquipmentDef equipment, double qualityWeight)
        {
            var power = product.Quality * qualityWeight;
            foreach (var w in equipment.StatWeights)
                foreach (var s in product.Stats)
                    if (s.StatId == w.StatId)
                        power += s.Value * w.Weight;
            return ScoreCalculator.Clamp(power, 0);
        }

        public static double OpponentPower(RunState state) =>
            ScoreCalculator.Clamp(state.Opponent.Power * SeasonModifiers.Scale(state, ScaledParam.OpponentPower) / 100, 0);

        /// <summary>Mức khó định tính ★1–5 suy từ Power đối thủ (đã áp modifier).</summary>
        public static int Stars(RunState state)
        {
            var power = OpponentPower(state);
            var stars = 1;
            foreach (var t in state.Def.StarThresholds)
                if (power >= t)
                    stars++;
            return stars;
        }
    }
}
