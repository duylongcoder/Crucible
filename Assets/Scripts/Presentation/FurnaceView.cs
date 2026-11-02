using System.Collections;
using System.Collections.Generic;
using System.Text;
using Alchemy.Core;
using UnityEngine;

namespace Alchemy.Presentation
{
    /// <summary>
    /// Lò nung: đặt/gỡ/di chuyển trên lưới, Nung, rồi phát lại <see cref="BrewLog"/> theo tick.
    /// Phát mọi tick từ nguyên liệu đầu tới nguyên liệu cuối; kết quả chỉ hiện sau tick cuối.
    /// </summary>
    public class FurnaceView : GameView
    {
        [SerializeField, Tooltip("Ô lưới theo thứ tự quét hàng.")]
        private ItemButton[] _cells;

        [SerializeField] private UnityEngine.UI.Button _brew;
        [SerializeField] private UnityEngine.UI.Button _skip;
        [SerializeField] private UnityEngine.UI.Button _continue;
        [SerializeField] private UnityEngine.UI.Button[] _speeds;
        [SerializeField] private UnityEngine.UI.Text _status;
        [SerializeField] private UnityEngine.UI.Text _log;

        [SerializeField, Min(0.1f), Tooltip("Giây mỗi tick ở tốc độ 1× (BrewTickSeconds).")]
        private float _brewTickSeconds = 3f;

        // Click đúp vào ô trống: lần 2 rơi vào ô vừa có đồ, không được hiểu là "gỡ ra".
        private const float DoubleClickSeconds = 0.4f;

        private int _timescale = 1;
        private WaitForSeconds[] _tickWaits;
        private int _lastPlacedCell = -1;
        private float _lastPlacedAt;
        private BrewResult _played;
        private bool _playing;
        private Coroutine _playback;
        private readonly StringBuilder _logText = new StringBuilder();

        protected override void CollectViewReferences(List<(string field, Object value)> references)
        {
            foreach (var c in _cells)
                references.Add((nameof(_cells), c));
            references.Add((nameof(_brew), _brew));
            references.Add((nameof(_skip), _skip));
            references.Add((nameof(_continue), _continue));
            foreach (var b in _speeds)
                references.Add((nameof(_speeds), b));
            references.Add((nameof(_status), _status));
            references.Add((nameof(_log), _log));
        }

        protected override void OnViewAwake()
        {
            _brew.onClick.AddListener(() => Game.Send(new BrewCommand()));
            _skip.onClick.AddListener(() => Game.Send(new SkipBrewCommand()));
            _continue.onClick.AddListener(() => Game.Send(new ContinueCommand()));
            _tickWaits = new WaitForSeconds[_speeds.Length];
            for (var i = 0; i < _speeds.Length; i++)
            {
                var speed = i + 1;
                _tickWaits[i] = new WaitForSeconds(_brewTickSeconds / speed);
                _speeds[i].onClick.AddListener(() => SetSpeed(speed));
            }
        }

        private void OnDisable()
        {
            // Ẩn giữa chừng thì coroutine đã dừng; nếu hiện lại ở Brewing thì phát lại từ đầu.
            _playback = null;
            _playing = false;
            _played = null;
        }

        protected override bool IsVisible(RunPhase phase) => phase == RunPhase.Placing || phase == RunPhase.Brewing;

        protected override void Render(RunState s)
        {
            var placing = s.Phase == RunPhase.Placing;
            _brew.gameObject.SetActive(placing);
            _skip.gameObject.SetActive(placing);
            _continue.gameObject.SetActive(!placing && !_playing);
            RenderSpeeds();

            if (placing)
            {
                _played = null;
                _status.text = $"Lượt nung {s.BrewsUsed + 1}/{s.Def.BrewsPerSeason}: xếp nguyên liệu rồi bấm Nung.";
                _log.text = "Lưới đọc trái → phải, trên → dưới. Ô trống là một nhịp chờ.";
                RenderGrid(s);
                return;
            }

            if (_played != s.LastBrew)
            {
                _played = s.LastBrew;
                if (_playback != null)
                    StopCoroutine(_playback);
                _playback = StartCoroutine(Play(_played));
            }
        }

        private void RenderGrid(RunState s)
        {
            var grid = s.Grid;
            for (var i = 0; i < _cells.Length; i++)
            {
                var item = grid[i / grid.Width, i % grid.Width];
                _cells[i].Bind(i, item == null ? "" : UiText.Item(item.Def), item == null ? UiText.SlotEmpty : UiText.ItemColor(item.Def),
                    CellClicked, source: ItemSource.Cell, draggable: item != null, dropped: Dropped);
            }
        }

        private void CellClicked(int cell)
        {
            var s = Game.State;
            int row = cell / s.Grid.Width, col = cell % s.Grid.Width;
            if (s.Grid[row, col] != null)
            {
                if (cell != _lastPlacedCell || Time.unscaledTime - _lastPlacedAt > DoubleClickSeconds)
                    Game.Send(new UnplaceCommand(row, col));
            }
            else if (Game.Selected != null && Game.Send(new PlaceCommand(Game.IndexInBag(Game.Selected), row, col)))
            {
                _lastPlacedCell = cell;
                _lastPlacedAt = Time.unscaledTime;
            }
        }

        private void Dropped(ItemButton source, int cell)
        {
            var w = Game.State.Grid.Width;
            if (source.Source == ItemSource.Bag)
                Game.Send(new PlaceCommand(source.Payload, cell / w, cell % w));
            else if (source.Source == ItemSource.Cell)
                Game.Send(new MoveCommand(source.Payload / w, source.Payload % w, cell / w, cell % w));
        }

        private IEnumerator Play(BrewResult brew)
        {
            _playing = true;
            _continue.gameObject.SetActive(false);
            _logText.Clear();

            var events = brew.Log.Events;
            for (var i = 0; i < _cells.Length; i++)
                _cells[i].Bind(i, "", UiText.SlotEmpty, null);
            foreach (var e in events)
                if (e.Kind == BrewEventKind.Feed)
                    _cells[e.Tick].Bind(e.Tick, Game.ItemName(e.ItemId), UiText.Slot, null);

            _status.text = events.Count == 0 ? "Lò trống — không có thành phẩm." : "Đang nung…";
            var tick = 0;
            foreach (var e in events)
            {
                if (e.Kind == BrewEventKind.Result)
                    break;
                tick++;
                _cells[e.Tick].Bind(e.Tick, e.ItemId == null ? "chờ" : Game.ItemName(e.ItemId), UiText.Active, null);
                _logText.Append("Nhịp ").Append(tick).Append(": ")
                    .Append(e.Kind == BrewEventKind.Feed ? "nạp " + Game.ItemName(e.ItemId) : "chờ").Append('\n');
                _log.text = _logText.ToString();

                // Đổi tốc độ có hiệu lực từ nhịp kế tiếp.
                yield return _tickWaits[_timescale - 1];
                _cells[e.Tick].Bind(e.Tick, e.ItemId == null ? "" : Game.ItemName(e.ItemId), UiText.SlotEmpty, null);
            }

            if (brew.Product != null)
                _status.text = brew.Product.Def.Kind == ItemKind.Scrap
                    ? "Ra: Phế phẩm."
                    : $"Ra: <b>{brew.Product.Def.Name}</b> (cấp {brew.Product.Def.Tier}).";

            _playing = false;
            _playback = null;
            _continue.gameObject.SetActive(true);
        }

        private void SetSpeed(int speed)
        {
            _timescale = speed;
            RenderSpeeds();
        }

        private void RenderSpeeds()
        {
            for (var i = 0; i < _speeds.Length; i++)
                _speeds[i].image.color = i + 1 == _timescale ? UiText.Selected : UiText.Slot;
        }
    }
}
