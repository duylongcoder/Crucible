using System.Collections.Generic;
using Alchemy.Core;
using UnityEngine;

namespace Alchemy.Presentation
{
    /// <summary>Panel Run mới: chọn bậc khó, bộ khởi đầu đã mở, seed. Hiện khi chưa có run dở và sau màn kết thúc.</summary>
    public class SetupView : ViewBehaviour
    {
        [SerializeField] private GameController _game;
        [SerializeField] private UnityEngine.UI.Text _renown;
        [SerializeField] private ButtonList _difficulties;
        [SerializeField] private ButtonList _kits;
        [SerializeField] private UnityEngine.UI.InputField _seed;
        [SerializeField] private UnityEngine.UI.Button _start;

        private DifficultyDef _difficulty;
        private StarterKitDef _kit;

        protected override void CollectRequiredReferences(List<(string field, Object value)> references)
        {
            references.Add((nameof(_game), _game));
            references.Add((nameof(_renown), _renown));
            references.Add((nameof(_difficulties), _difficulties));
            references.Add((nameof(_kits), _kits));
            references.Add((nameof(_seed), _seed));
            references.Add((nameof(_start), _start));
        }

        protected override void OnViewAwake() =>
            _start.onClick.AddListener(() => _game.StartRun(_difficulty, _kit, _seed.text));

        public void Show()
        {
            gameObject.SetActive(true);
            _seed.text = "";
            var def = _game.Def;
            var meta = _game.Meta.State;
            // Mặc định: bậc khó cao nhất đã mở, bộ khởi đầu đầu tiên.
            _difficulty = def.Difficulties[System.Math.Min(meta.UnlockedDifficulties, def.Difficulties.Count) - 1];
            _kit = def.StarterKits[0];
            Render();
        }

        public void Hide() => gameObject.SetActive(false);

        private void Render()
        {
            var def = _game.Def;
            var meta = _game.Meta.State;
            _renown.text = $"Danh vọng: {meta.Renown}" + NextUnlock(def, meta.Renown);

            _difficulties.Show(def.Difficulties.Count, (b, i) =>
            {
                var d = def.Difficulties[i];
                var open = meta.IsDifficultyUnlocked(def, d);
                var detail = open ? $"Danh vọng ×{d.RenownPercent}%" : "Khóa: thắng run ở bậc trước";
                b.Bind(i, $"{d.Name}\n<size=18>{detail}</size>", d == _difficulty ? UiText.Selected : UiText.Slot,
                    index => Select(def.Difficulties[index], _kit), open);
            });

            _kits.Show(def.StarterKits.Count, (b, i) =>
            {
                var k = def.StarterKits[i];
                var open = k.UnlockRenown <= meta.Renown;
                var detail = open ? k.Description : $"Khóa: cần {k.UnlockRenown} Danh vọng";
                b.Bind(i, $"{k.Name}\n<size=18>{detail}</size>", k == _kit ? UiText.Selected : UiText.Slot,
                    index => Select(_difficulty, def.StarterKits[index]), open);
            });
        }

        private void Select(DifficultyDef difficulty, StarterKitDef kit)
        {
            _difficulty = difficulty;
            _kit = kit;
            Render();
        }

        /// <summary>Mốc mở khóa gần nhất chưa đạt, để người chơi biết đang hướng tới gì.</summary>
        private static string NextUnlock(RunDef def, int renown)
        {
            string name = null;
            var best = int.MaxValue;
            void Check(int threshold, string n)
            {
                if (threshold > renown && threshold < best)
                {
                    best = threshold;
                    name = n;
                }
            }

            foreach (var k in def.StarterKits)
                Check(k.UnlockRenown, k.Name);
            foreach (var item in def.ShopPool)
                Check(item.UnlockRenown, item.Name + " vào Chợ");
            foreach (var r in def.Rewards)
                Check(r.UnlockRenown, r.Name);
            return name == null ? "" : $"   ·   Mở tiếp: {name} ({best})";
        }
    }
}
