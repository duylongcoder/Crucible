using System.Collections.Generic;
using Alchemy.Core;
using UnityEngine;

namespace Alchemy.Presentation
{
    /// <summary>Túi + chi tiết vật phẩm đang chọn + các hành động hợp lệ theo phase (Bán, Dâng, Soi).</summary>
    public class BagView : GameView
    {
        [SerializeField] private UnityEngine.UI.Text _title;
        [SerializeField] private ButtonList _items;
        [SerializeField] private DropZone _dropZone;
        [SerializeField] private UnityEngine.UI.Text _detail;
        [SerializeField] private ButtonList _actions;

        private readonly List<Product> _shown = new List<Product>();
        private readonly List<(string label, ICommand command)> _actionList = new List<(string, ICommand)>();

        protected override void CollectViewReferences(List<(string field, Object value)> references)
        {
            references.Add((nameof(_title), _title));
            references.Add((nameof(_items), _items));
            references.Add((nameof(_dropZone), _dropZone));
            references.Add((nameof(_detail), _detail));
            references.Add((nameof(_actions), _actions));
        }

        protected override void OnViewAwake()
        {
            _dropZone.Dropped = Unplace;
        }

        protected override bool IsVisible(RunPhase phase) => phase != RunPhase.RunEnd;

        protected override void Render(RunState s)
        {
            // Thành phẩm và vật phẩm Xúc tác tạo ra chỉ lộ sau khi phát lại xong (Continue khỏi Brewing).
            var brew = s.Phase == RunPhase.Brewing ? s.LastBrew : null;
            _shown.Clear();
            foreach (var p in s.Bag)
                if (brew == null || (p != brew.Product && !Contains(brew.Created, p)))
                    _shown.Add(p);

            _title.text = $"Túi {_shown.Count}/{s.BagCapacity}";
            var placing = s.Phase == RunPhase.Placing;
            _items.Show(_shown.Count, (b, i) =>
            {
                var p = _shown[i];
                b.Bind(i, UiText.Item(p.Def), p == Game.Selected ? UiText.Selected : UiText.ItemColor(p.Def),
                    _ => Game.Select(p), source: ItemSource.Bag, draggable: placing, dropped: (src, __) => Unplace(src));
            });

            RenderDetail(s);
        }

        private void RenderDetail(RunState s)
        {
            _actionList.Clear();
            var p = Game.Selected;
            if (p == null)
            {
                _detail.text = s.Phase == RunPhase.Placing ? "Kéo vật phẩm lên lưới, hoặc chọn rồi bấm ô trống." : "Chọn vật phẩm để xem.";
                _actions.Show(0, null);
                return;
            }

            _detail.text = $"<b>{p.Def.Name}</b>\n{UiText.Detail(p, Game.StatName)}";
            var index = Game.IndexInBag(p);
            if (s.Phase == RunPhase.Market)
                _actionList.Add(($"Bán (+{Market.SellPrice(s, p.Def)} vàng)", new SellCommand(index)));
            if (s.Phase == RunPhase.Offering && p.Def.IsBrewed)
                _actionList.Add(("Dâng khoáng này", new OfferCommand(index)));
            if (s.Phase != RunPhase.Brewing && p.CanInspect)
                AddLensActions(s, index);

            _actions.Show(_actionList.Count, (b, i) =>
            {
                var command = _actionList[i].command;
                b.Bind(i, _actionList[i].label, UiText.Slot, _ => Game.Send(command));
            });
        }

        // Mỗi loại Kính soi trong Túi một nút.
        private void AddLensActions(RunState s, int targetIndex)
        {
            var seen = new List<ItemDef>();
            for (var i = 0; i < s.Bag.Count; i++)
            {
                var def = s.Bag[i].Def;
                if (def.Kind != ItemKind.Lens || seen.Contains(def))
                    continue;
                seen.Add(def);
                _actionList.Add(($"Soi bằng {def.Name}", new InspectCommand(i, targetIndex)));
            }
        }

        private static bool Contains(IReadOnlyList<Product> list, Product p)
        {
            foreach (var x in list)
                if (x == p)
                    return true;
            return false;
        }

        private void Unplace(ItemButton source)
        {
            if (source.Source != ItemSource.Cell)
                return;
            var width = Game.State.Grid.Width;
            Game.Send(new UnplaceCommand(source.Payload / width, source.Payload % width));
        }
    }
}
