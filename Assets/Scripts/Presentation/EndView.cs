using System.Collections.Generic;
using Alchemy.Core;
using UnityEngine;

namespace Alchemy.Presentation
{
    public class EndView : GameView
    {
        [SerializeField] private UnityEngine.UI.Text _title;
        [SerializeField] private UnityEngine.UI.Text _summary;
        [SerializeField] private UnityEngine.UI.Button _newRun;

        protected override void CollectViewReferences(List<(string field, Object value)> references)
        {
            references.Add((nameof(_title), _title));
            references.Add((nameof(_summary), _summary));
            references.Add((nameof(_newRun), _newRun));
        }

        protected override void OnViewAwake() => _newRun.onClick.AddListener(() => Game.NewRun());

        protected override bool IsVisible(RunPhase phase) => phase == RunPhase.RunEnd;

        protected override void Render(RunState s)
        {
            var won = s.Outcome == RunOutcome.Won;
            _title.text = won ? "Hoàn thành 8 Mùa" : "Bị trục xuất";
            _summary.text = won
                ? $"Bạn giữ được lòng tin của Nhà vua tới Mùa cuối.\nThắng {s.Wins}/{s.Def.SeasonsPerRun} trận, còn {s.Reputation} Uy tín."
                : $"Uy tín cạn ở Mùa {s.Season}. Thắng {s.Wins} trận.";
        }
    }
}
