using System.Collections.Generic;
using Alchemy.Core;
using UnityEngine;

namespace Alchemy.Presentation
{
    /// <summary>Xưởng (soi, chọn; không mua bán) và Dâng (chọn thành phẩm trong Túi).</summary>
    public class WorkshopView : GameView
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

        protected override bool IsVisible(RunPhase phase) => phase == RunPhase.Workshop || phase == RunPhase.Offering;

        protected override void Render(RunState s)
        {
            var workshop = s.Phase == RunPhase.Workshop;
            var o = s.Opponent;
            _title.text = workshop ? "Xưởng" : "Dâng khoáng";
            _body.text = workshop
                ? "Hết 3 lượt nung. Dùng Kính soi để xem thuộc tính ẩn của thành phẩm (chọn thành phẩm trong Túi).\n" +
                  "Xưởng không mua bán.\n\n" +
                  $"Đối thủ: {o.Name} — {UiText.Battle(o.Kind)} {UiText.Stars(Game.Machine.OpponentStars)}"
                : "Chọn một thành phẩm đạt chuẩn trong Túi rồi bấm \"Dâng khoáng này\".\n" +
                  "Khoáng dâng luôn bị tiêu hao. Loại trang bị được rèn ngẫu nhiên sau khi dâng.";
            _continue.gameObject.SetActive(workshop);
        }
    }
}
