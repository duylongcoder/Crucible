using System.Collections.Generic;
using Alchemy.Core;
using UnityEngine;

namespace Alchemy.Presentation
{
    public class MarketView : GameView
    {
        [SerializeField] private UnityEngine.UI.Text _title;
        [SerializeField] private ButtonList _offers;
        [SerializeField] private UnityEngine.UI.Button _reroll;
        [SerializeField] private UnityEngine.UI.Text _rerollLabel;
        [SerializeField] private UnityEngine.UI.Button _toFurnace;
        [SerializeField] private UnityEngine.UI.Button _skip;

        protected override void CollectViewReferences(List<(string field, Object value)> references)
        {
            references.Add((nameof(_title), _title));
            references.Add((nameof(_offers), _offers));
            references.Add((nameof(_reroll), _reroll));
            references.Add((nameof(_rerollLabel), _rerollLabel));
            references.Add((nameof(_toFurnace), _toFurnace));
            references.Add((nameof(_skip), _skip));
        }

        protected override void OnViewAwake()
        {
            _reroll.onClick.AddListener(() => Game.Send(new RerollCommand()));
            _toFurnace.onClick.AddListener(() => Game.Send(new LeaveMarketCommand()));
            _skip.onClick.AddListener(() => Game.Send(new SkipBrewCommand()));
        }

        protected override bool IsVisible(RunPhase phase) => phase == RunPhase.Market;

        protected override void Render(RunState s)
        {
            _title.text = $"Chợ — trước lượt nung {s.BrewsUsed + 1}/{s.Def.BrewsPerSeason}   <size=20>(chọn vật phẩm trong Túi để bán)</size>";
            var offers = s.Offers;
            _offers.Show(offers.Count, (b, i) =>
                b.Bind(i, offers[i].Sold ? "Đã bán" : $"{UiText.Item(offers[i].Item)}\n{offers[i].Price} vàng",
                    offers[i].Sold ? UiText.SlotEmpty : UiText.Slot,
                    index => Game.Send(new BuyCommand(index)), interactable: !offers[i].Sold && s.Gold >= offers[i].Price));
            _rerollLabel.text = $"Đổi hàng ({Market.RerollCost(s)} vàng)";
        }
    }
}
