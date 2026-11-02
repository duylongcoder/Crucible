namespace Alchemy.Core
{
    internal static class SeasonModifiers
    {
        /// <summary>Hệ số (%) mà modifier hiện tại áp lên tham số; 100 khi không có modifier.</summary>
        public static int Scale(RunState state, ScaledParam param, string targetId = null)
        {
            var percent = 100;
            if (state.Modifier == null)
                return percent;

            foreach (var s in state.Modifier.Scales)
                if (s.Param == param && (s.TargetId == null || s.TargetId == targetId))
                    percent = percent * s.Percent / 100;
            return percent;
        }

        public static int FreeRerolls(RunState state) => state.Modifier?.FreeRerolls ?? 0;

        /// <summary>0–1 modifier cho Mùa sau (stream Season).</summary>
        public static SeasonModifierDef RollNext(RunState state)
        {
            var def = state.Def;
            var rng = state.Rng[RngStream.Season];
            if (def.Modifiers.Count == 0 || rng.NextInt(100) >= def.ModifierChancePercent)
                return null;
            return def.Modifiers[rng.NextInt(def.Modifiers.Count)];
        }
    }
}
