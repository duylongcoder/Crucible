using System.Collections;
using System.Collections.Generic;
using System.Text;
using Alchemy.Core;
using UnityEngine;

namespace Alchemy.Presentation
{
    /// <summary>Rèn rồi Trận: chỉ hiện loại trang bị và Thắng/Thua, không có con số sức mạnh.</summary>
    public class BattleView : GameView
    {
        [SerializeField] private UnityEngine.UI.Text _lines;
        [SerializeField] private UnityEngine.UI.Text _verdict;
        [SerializeField] private UnityEngine.UI.Button _continue;
        [SerializeField, Min(0.1f)] private float _stepSeconds = 0.9f;

        private BattleResult _played;
        private readonly StringBuilder _text = new StringBuilder();

        protected override void CollectViewReferences(List<(string field, Object value)> references)
        {
            references.Add((nameof(_lines), _lines));
            references.Add((nameof(_verdict), _verdict));
            references.Add((nameof(_continue), _continue));
        }

        protected override void OnViewAwake() => _continue.onClick.AddListener(() => Game.Send(new ContinueCommand()));

        private void OnDisable() => _played = null;

        protected override bool IsVisible(RunPhase phase) => phase == RunPhase.Battle;

        protected override void Render(RunState s)
        {
            if (_played == s.LastBattle)
                return;
            _played = s.LastBattle;
            StopAllCoroutines();
            StartCoroutine(Play(s, _played));
        }

        private IEnumerator Play(RunState s, BattleResult b)
        {
            _continue.gameObject.SetActive(false);
            _verdict.text = "";
            _text.Clear();
            var wait = new WaitForSeconds(_stepSeconds);

            if (b.Offered == null)
            {
                Line("Không có thành phẩm nào để dâng.");
                yield return wait;
            }
            else
            {
                Line($"Dâng: {b.Offered.Name}");
                yield return wait;
                Line("Lò rèn đỏ lửa…");
                yield return wait;
                Line($"Rèn ra: <b>{b.Equipment.Name}</b>");
                yield return wait;
            }

            Line($"{UiText.Battle(s.Opponent.Kind)} với {s.Opponent.Name}…");
            yield return wait;

            _verdict.text = b.Won ? "THẮNG" : "THUA";
            _verdict.color = b.Won ? UiText.Selected : UiText.ScrapColor * 2f;
            if (!b.Won)
                Line(s.Outcome == RunOutcome.Exiled ? "Uy tín cạn. Nhà vua trục xuất bạn." : "Uy tín −1.");
            _continue.gameObject.SetActive(true);
        }

        private void Line(string line)
        {
            _text.Append(line).Append('\n');
            _lines.text = _text.ToString();
        }
    }
}
