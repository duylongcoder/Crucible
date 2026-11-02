using System.Collections;
using System.Collections.Generic;
using Alchemy.Content;
using Alchemy.Core;
using UnityEngine;

namespace Alchemy.Presentation
{
    /// <summary>
    /// Giữ <see cref="RunStateMachine"/>, gửi Command và làm mới mọi view. Không chứa luật chơi.
    /// </summary>
    public class GameController : ViewBehaviour
    {
        [SerializeField] private GameView[] _views;
        [SerializeField] private UnityEngine.UI.Text _toast;
        [SerializeField] private RectTransform _dragGhost;
        [SerializeField] private UnityEngine.UI.Text _dragGhostLabel;
        [SerializeField, Min(0.5f)] private float _toastSeconds = 2.5f;

        private RunDef _def;
        private Coroutine _toastRoutine;
        private readonly Dictionary<string, ItemDef> _itemsById = new Dictionary<string, ItemDef>();

        public RunStateMachine Machine { get; private set; }
        public RunState State => Machine.State;

        /// <summary>Vật phẩm đang chọn trong Túi (giữ theo instance vì chỉ số Túi đổi sau mỗi Command).</summary>
        public Product Selected { get; private set; }

        /// <summary>Ô đang kéo; ô nhận thả đọc từ đây thay vì GetComponent trên pointerDrag.</summary>
        public ItemButton Dragging { get; private set; }

        protected override void CollectRequiredReferences(List<(string field, Object value)> references)
        {
            references.Add((nameof(_toast), _toast));
            references.Add((nameof(_dragGhost), _dragGhost));
            references.Add((nameof(_dragGhostLabel), _dragGhostLabel));
            foreach (var v in _views)
                references.Add((nameof(_views), v));
        }

        protected override void OnViewAwake()
        {
            _def = DemoContent.CreateRun();
            foreach (var item in _def.Items)
                _itemsById[item.Id] = item;
            _itemsById[_def.Scrap.Id] = _def.Scrap;
        }

        private void Start()
        {
            if (enabled)
                NewRun();
        }

        public void NewRun()
        {
            // shortcut: seed ngẫu nhiên mỗi lần; chọn/nhập seed và lưu run ở M4–M5.
            var seed = (ulong)(uint)System.Environment.TickCount;
            Machine = new RunStateMachine(_def, seed, checkInvariants: Debug.isDebugBuild);
            Selected = null;
            RefreshAll();
        }

        public bool Send(ICommand command)
        {
            var result = Machine.Execute(command);
            if (!result.Accepted)
            {
                ShowToast(result.Error);
                return false;
            }

            if (Selected != null && IndexInBag(Selected) < 0)
                Selected = null;
            RefreshAll();
            return true;
        }

        public void Select(Product product)
        {
            Selected = Selected == product ? null : product;
            RefreshAll();
        }

        public int IndexInBag(Product product)
        {
            var bag = State.Bag;
            for (var i = 0; i < bag.Count; i++)
                if (bag[i] == product)
                    return i;
            return -1;
        }

        public string ItemName(string itemId) => _itemsById.TryGetValue(itemId, out var def) ? def.Name : itemId;

        public void RefreshAll()
        {
            foreach (var v in _views)
                v.Refresh(State);
        }

        public void ShowToast(string message)
        {
            if (_toastRoutine != null)
                StopCoroutine(_toastRoutine);
            _toastRoutine = StartCoroutine(Toast(message));
        }

        public void BeginDrag(ItemButton source, Vector2 screenPosition)
        {
            Dragging = source;
            _dragGhostLabel.text = source.Label;
            _dragGhost.gameObject.SetActive(true);
            MoveDrag(screenPosition);
        }

        public void MoveDrag(Vector2 screenPosition) => _dragGhost.position = screenPosition;

        public void EndDrag()
        {
            Dragging = null;
            _dragGhost.gameObject.SetActive(false);
        }

        private IEnumerator Toast(string message)
        {
            _toast.text = message;
            _toast.gameObject.SetActive(true);
            yield return new WaitForSeconds(_toastSeconds);
            _toast.gameObject.SetActive(false);
            _toastRoutine = null;
        }
    }
}
