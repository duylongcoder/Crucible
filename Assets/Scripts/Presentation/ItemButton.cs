using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Alchemy.Presentation
{
    public enum ItemSource
    {
        None,
        Bag,
        Cell,
    }

    /// <summary>Ô bấm được dùng cho vật phẩm Túi, ô lưới, hàng Chợ, lựa chọn. Kéo thả khi <see cref="Draggable"/>.</summary>
    public class ItemButton : ViewBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] private UnityEngine.UI.Button _button;
        [SerializeField] private UnityEngine.UI.Text _label;
        [SerializeField] private UnityEngine.UI.Image _background;
        [SerializeField] private GameController _game;

        private Action<int> _clicked;
        private Action<ItemButton, int> _dropped;

        public int Payload { get; private set; }
        public ItemSource Source { get; private set; }
        public bool Draggable { get; private set; }
        public string Label => _label.text;

        protected override void CollectRequiredReferences(List<(string field, UnityEngine.Object value)> references)
        {
            references.Add((nameof(_button), _button));
            references.Add((nameof(_label), _label));
            references.Add((nameof(_background), _background));
            references.Add((nameof(_game), _game));
        }

        protected override void OnViewAwake() => _button.onClick.AddListener(() => _clicked?.Invoke(Payload));

        /// <summary>Gán lại toàn bộ trạng thái (dùng lại từ pool nên không cộng dồn callback).</summary>
        public void Bind(int payload, string label, Color color, Action<int> clicked, bool interactable = true,
            ItemSource source = ItemSource.None, bool draggable = false, Action<ItemButton, int> dropped = null)
        {
            Payload = payload;
            Source = source;
            Draggable = draggable;
            _label.text = label;
            _background.color = color;
            _clicked = clicked;
            _dropped = dropped;
            _button.interactable = interactable;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!Draggable)
            {
                eventData.pointerDrag = null;
                return;
            }

            _game.BeginDrag(this, eventData.position);
        }

        public void OnDrag(PointerEventData eventData) => _game.MoveDrag(eventData.position);

        public void OnEndDrag(PointerEventData eventData) => _game.EndDrag();

        public void OnDrop(PointerEventData eventData)
        {
            var source = _game.Dragging;
            if (source != null && source != this && source.Draggable)
                _dropped?.Invoke(source, Payload);
        }
    }
}
