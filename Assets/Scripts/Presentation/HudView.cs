using System.Collections.Generic;
using Alchemy.Core;
using UnityEngine;

namespace Alchemy.Presentation
{
    public class HudView : GameView
    {
        [SerializeField] private UnityEngine.UI.Text _info;
        [SerializeField] private UnityEngine.UI.Text _modifier;

        protected override void CollectViewReferences(List<(string field, Object value)> references)
        {
            references.Add((nameof(_info), _info));
            references.Add((nameof(_modifier), _modifier));
        }

        protected override bool IsVisible(RunPhase phase) => true;

        protected override void Render(RunState s)
        {
            // Trong lúc animation trận, HUD giữ Uy tín trước trận để không lộ kết quả sớm.
            var reputation = s.Phase == RunPhase.Battle && s.LastBattle != null && !s.LastBattle.Won ? s.Reputation + 1 : s.Reputation;
            // Vàng Xúc tác cho trong lượt nung cũng chờ phát lại xong.
            var gold = s.Phase == RunPhase.Brewing && s.LastBrew != null ? s.Gold - s.LastBrew.GoldGained : s.Gold;
            var hearts = new string('♥', reputation) + new string('♡', Mathf.Max(0, s.Def.StartReputation - reputation));
            _info.text = $"Mùa {s.Season}/{s.Def.SeasonsPerRun}   ·   {UiText.Phase(s.Phase)}   ·   Lượt {s.BrewsUsed}/{s.Def.BrewsPerSeason}" +
                         $"   ·   {gold} vàng   ·   Uy tín {hearts}";
            _modifier.text = $"Mùa này: {UiText.Modifier(s.Modifier)}\n<size=16>seed {s.Seed}</size>";
        }
    }
}
