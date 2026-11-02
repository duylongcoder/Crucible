using System.Collections.Generic;
using Alchemy.Core;
using UnityEngine;

namespace Alchemy.Presentation
{
    /// <summary>Chiếu chỉ: đối thủ Mùa này (tên, loại trận, mức khó định tính) và modifier Mùa này / Mùa sau.</summary>
    public class DecreeView : GameView
    {
        [SerializeField] private UnityEngine.UI.Text _title;
        [SerializeField] private UnityEngine.UI.Text _body;
        [SerializeField] private UnityEngine.UI.Button _continue;

        protected override void CollectViewReferences(List<(string field, Object value)> references)
        {
            references.Add((nameof(_title), _title));
            references.Add((nameof(_body), _body));
            references.Add((nameof(_continue), _continue));
        }

        protected override void OnViewAwake() => _continue.onClick.AddListener(() => Game.Send(new ContinueCommand()));

        protected override bool IsVisible(RunPhase phase) => phase == RunPhase.Decree;

        protected override void Render(RunState s)
        {
            var o = s.Opponent;
            _title.text = $"Chiếu chỉ — Mùa {s.Season}";
            _body.text =
                $"Nhà vua truyền: cuối Mùa này, binh lính sẽ ra {UiText.Battle(o.Kind).ToLowerInvariant()}.\n\n" +
                $"Đối thủ: <b>{o.Name}</b>\nLoại trận: {UiText.Battle(o.Kind)}\nĐộ khó: {UiText.Stars(Game.Machine.OpponentStars)}\n\n" +
                $"Modifier Mùa này: {UiText.Modifier(s.Modifier)}\n" +
                (s.Season < s.Def.SeasonsPerRun ? $"Modifier Mùa sau: {UiText.Modifier(s.NextModifier)}" : "Đây là Mùa cuối.");
        }
    }
}
