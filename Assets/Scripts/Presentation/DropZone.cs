using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Alchemy.Presentation
{
    /// <summary>Vùng nhận thả (vd. nền Túi: thả ô lưới vào để gỡ). Cần một Graphic raycast được trên cùng object.</summary>
    public class DropZone : MonoBehaviour, IDropHandler
    {
        [SerializeField] private GameController _game;

        public Action<ItemButton> Dropped { get; set; }

        public void OnDrop(PointerEventData eventData)
        {
            var source = _game.Dragging;
            if (source != null && source.Draggable)
                Dropped?.Invoke(source);
        }
    }
}
