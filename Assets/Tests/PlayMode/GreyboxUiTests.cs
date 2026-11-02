using System.Collections;
using System.Linq;
using Alchemy.Core;
using Alchemy.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Alchemy.Tests.PlayMode
{
    /// <summary>
    /// Test UI qua đường của click thật (ExecuteEvents → Button.OnPointerClick tôn trọng interactable/active):
    /// spam nút, kéo thả, chơi trọn một run, không lộ thông tin ẩn.
    /// </summary>
    public class GreyboxUiTests
    {
        private const string Stage = "Canvas/Game/Stage/";
        private GameController _game;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            _game = Object.FindAnyObjectByType<GameController>();
            Assert.That(_game, Is.Not.Null);
        }

        [TearDown]
        public void TearDown() => Time.timeScale = 1f;

        // ---- helpers ----

        /// <summary>
        /// GameObject.Find với đường dẫn có '/' vẫn trả về con đang inactive, nên phải lọc activeInHierarchy.
        /// </summary>
        private static GameObject Active(string path)
        {
            var go = GameObject.Find(path);
            return go != null && go.activeInHierarchy ? go : null;
        }

        private static GameObject Find(string path)
        {
            var go = Active(path);
            Assert.That(go, Is.Not.Null, $"Không thấy object active '{path}'");
            return go;
        }

        private static void Click(GameObject go) =>
            ExecuteEvents.Execute(go, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left },
                ExecuteEvents.pointerClickHandler);

        private static void Click(string path) => Click(Find(path));

        /// <summary>Kéo thật: BeginDrag trên nguồn → Drop trên đích → EndDrag trên nguồn (thứ tự của input module).</summary>
        private static void Drop(GameObject source, GameObject target)
        {
            var e = new PointerEventData(EventSystem.current) { pointerDrag = source };
            ExecuteEvents.Execute(source, e, ExecuteEvents.beginDragHandler);
            if (e.pointerDrag != null)
                ExecuteEvents.Execute(target, e, ExecuteEvents.dropHandler);
            ExecuteEvents.Execute(source, e, ExecuteEvents.endDragHandler);
        }

        private static ItemButton BagButton(string itemName) =>
            Find("Canvas/Game/Bag/Items/Container").GetComponentsInChildren<ItemButton>()
                .FirstOrDefault(b => b.Label.StartsWith(itemName));

        private static ItemButton ActionButton(string prefix) =>
            Find("Canvas/Game/Bag/Actions/Container").GetComponentsInChildren<ItemButton>()
                .FirstOrDefault(b => b.Label.StartsWith(prefix));

        private static GameObject Cell(int i) => Find(Stage + "Furnace/Grid/Cell" + i);

        private static IEnumerator WaitFor(System.Func<bool> condition, float timeout = 30f)
        {
            var end = Time.realtimeSinceStartup + timeout;
            while (!condition())
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(end), "Hết thời gian chờ");
                yield return null;
            }
        }

        private void ToPlacing()
        {
            Click(Stage + "Decree/Continue");
            Click(Stage + "Market/ToFurnace");
            Assert.That(_game.State.Phase, Is.EqualTo(RunPhase.Placing));
        }

        // ---- spam ----

        [Test]
        public void SpamClick_OpenMarket_EntersMarketOnce()
        {
            var button = Find(Stage + "Decree/Continue");
            for (var i = 0; i < 20; i++)
                Click(button);

            Assert.That(_game.State.Phase, Is.EqualTo(RunPhase.Market));
            Assert.That(_game.State.BrewsUsed, Is.EqualTo(0));
        }

        [Test]
        public void SpamClick_SameOfferSlot_BuysOnlyThatOffer()
        {
            Click(Stage + "Decree/Continue");
            _game.State.Gold = 500;
            // Túi khởi đầu gần đầy: nới ra để chỉ còn luật "Đã bán" chặn lần mua thứ hai.
            _game.State.BagCapacity = _game.State.Def.MaxBagCapacity;
            _game.RefreshAll();
            var container = Find(Stage + "Market/Offers/Container").transform;
            var bag = _game.State.Bag.Count;

            // Chuột thật bấm theo vị trí: luôn là ô đầu tiên của layout, dù GameObject sau đó là món khác.
            for (var i = 0; i < 10; i++)
            {
                var first = container.Cast<Transform>().FirstOrDefault(t => t.gameObject.activeSelf);
                if (first != null)
                    Click(first.gameObject);
            }

            Assert.That(_game.State.Bag.Count, Is.EqualTo(bag + 1), "Spam một ô hàng chỉ được mua đúng món đó");
        }

        [Test]
        public void SpamClick_Brew_BrewsOnce()
        {
            ToPlacing();
            Click(BagButton("Quặng sắt").gameObject);
            Click(Cell(0));
            var brew = Find(Stage + "Furnace/Brew");

            for (var i = 0; i < 20; i++)
                Click(brew);

            Assert.That(_game.State.Phase, Is.EqualTo(RunPhase.Brewing));
            Assert.That(_game.State.BrewsUsed, Is.EqualTo(1));
        }

        [Test]
        public void DoubleClick_EmptyCellWithSelection_PlacesAndKeepsItem()
        {
            ToPlacing();
            Click(BagButton("Quặng sắt").gameObject);
            var cell = Cell(4);

            Click(cell);
            Click(cell);

            Assert.That(_game.State.Grid[1, 1], Is.Not.Null, "Click đúp không được tự gỡ vật phẩm vừa đặt");
        }

        [UnityTest]
        public IEnumerator SpamClick_ContinueDuringBrewPlayback_Ignored()
        {
            ToPlacing();
            Click(BagButton("Quặng sắt").gameObject);
            Click(Cell(0));
            Click(BagButton("Than đá").gameObject);
            Click(Cell(2));
            Click(Stage + "Furnace/Brew");
            yield return null;

            var cont = Active(Stage + "Furnace/Continue");
            for (var i = 0; i < 20; i++)
                if (cont != null)
                    Click(cont);

            Assert.That(_game.State.Phase, Is.EqualTo(RunPhase.Brewing), "Không được qua lượt trước khi phát xong");
            Assert.That(_game.State.LastBrew.Product.Def.Id, Is.EqualTo("steel"));
            Assert.That(BagButton("Thép"), Is.Null, "Thành phẩm chưa được lộ trong Túi khi đang phát");

            Time.timeScale = 20f;
            yield return WaitFor(() => Active(Stage + "Furnace/Continue") != null);
            Assert.That(BagButton("Thép"), Is.Null, "Vẫn ẩn tới khi bấm Tiếp tục");
            Click(Stage + "Furnace/Continue");
            Assert.That(_game.State.Phase, Is.EqualTo(RunPhase.Market));
            Assert.That(BagButton("Thép"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator SpamClick_ContinueDuringBattleAnimation_Ignored_ReputationHiddenUntilVerdict()
        {
            Click(Stage + "Decree/Continue");
            for (var i = 0; i < 3; i++)
                Click(Stage + "Market/Skip");
            Click(Stage + "Workshop/Continue"); // không có thành phẩm → tự động thua
            Assert.That(_game.State.Phase, Is.EqualTo(RunPhase.Battle));

            var cont = GameObject.Find(Stage + "Battle/Continue");
            for (var i = 0; i < 20; i++)
                Click(cont);
            Assert.That(cont.activeInHierarchy, Is.False, "Nút Tiếp tục phải ẩn trong lúc animation");
            Assert.That(_game.State.Phase, Is.EqualTo(RunPhase.Battle));
            Assert.That(Find("Canvas/Game/Hud/Info").GetComponent<Text>().text, Does.Contain("♥♥♥"),
                "HUD không được lộ kết quả trước animation");

            Time.timeScale = 20f;
            yield return WaitFor(() => cont.activeInHierarchy);
            Click(cont);
            Click(cont);
            Assert.That(_game.State.Season, Is.EqualTo(2), "Spam Tiếp tục chỉ qua một Mùa");
            Assert.That(_game.State.Phase, Is.EqualTo(RunPhase.Decree));
        }

        // ---- kéo thả + input xấu ----

        [Test]
        public void Drag_BagToCell_CellToCell_CellToBag()
        {
            ToPlacing();
            Drop(BagButton("Quặng sắt").gameObject, Cell(0));
            Assert.That(_game.State.Grid[0, 0].Def.Id, Is.EqualTo("iron_ore"));

            Drop(Cell(0), Cell(8));
            Assert.That(_game.State.Grid[0, 0], Is.Null);
            Assert.That(_game.State.Grid[2, 2].Def.Id, Is.EqualTo("iron_ore"));

            Drop(Cell(8), Find("Canvas/Game/Bag"));
            Assert.That(_game.State.Grid[2, 2], Is.Null);
            Assert.That(_game.State.Bag.Count, Is.EqualTo(13));
        }

        [Test]
        public void Drag_LensOntoCell_RejectedWithToast()
        {
            ToPlacing();
            Drop(BagButton("Kính soi").gameObject, Cell(0));

            Assert.That(_game.State.Grid[0, 0], Is.Null);
            Assert.That(Active("Canvas/Game/Toast"), Is.Not.Null, "Lệnh bị từ chối phải báo cho người chơi");
        }

        [Test]
        public void Drag_InMarket_NotDraggable()
        {
            Click(Stage + "Decree/Continue");
            var iron = BagButton("Quặng sắt");

            Assert.That(iron.Draggable, Is.False);
        }

        [Test]
        public void Click_EmptyCellWithoutSelection_DoesNothing()
        {
            ToPlacing();
            var hash = _game.State.ComputeHash();

            Click(Cell(3));

            Assert.That(_game.State.ComputeHash(), Is.EqualTo(hash));
        }

        [Test]
        public void Sell_SpamClickSellAction_SellsOnlySelectedItem()
        {
            Click(Stage + "Decree/Continue");
            Click(BagButton("Hàn the").gameObject);
            var sell = ActionButton("Bán");
            Assert.That(sell, Is.Not.Null);
            var bag = _game.State.Bag.Count;

            for (var i = 0; i < 10; i++)
                Click(sell.gameObject);

            Assert.That(_game.State.Bag.Count, Is.EqualTo(bag - 1));
        }

        // ---- trọn run qua UI ----

        private static readonly string[] Forbidden =
        {
            "Power", "power", "Essence", "Potency", "Gap", "gap", "RequiresMagic", "NaN", "Infinity", "Exception", "Weight",
        };

        private static void AssertNoLeak()
        {
            foreach (var t in Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
                foreach (var word in Forbidden)
                    Assert.That(t.text, Does.Not.Contain(word), $"Text '{t.name}' lộ '{word}'");
        }

        [UnityTest]
        public IEnumerator FullRun_ByClicksOnly_ReachesRunEnd_ThenNewRunRestarts()
        {
            Time.timeScale = 50f;
            var deadline = Time.realtimeSinceStartup + 240f;
            var phasesSeen = new System.Collections.Generic.HashSet<RunPhase>();

            while (_game.State.Phase != RunPhase.RunEnd)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), $"Kẹt ở {_game.State.Phase}");
                AssertNoLeak();
                var s = _game.State;
                phasesSeen.Add(s.Phase);
                switch (s.Phase)
                {
                    case RunPhase.Decree:
                        Click(Stage + "Decree/Continue");
                        break;
                    case RunPhase.Market:
                        Click(Stage + "Market/ToFurnace");
                        break;
                    case RunPhase.Placing:
                        var iron = BagButton("Quặng sắt");
                        var coal = BagButton("Than đá");
                        if (iron != null && coal != null)
                        {
                            Click(iron.gameObject);
                            Click(Cell(0));
                            Click(BagButton("Than đá").gameObject);
                            Click(Cell(2));
                            Click(Stage + "Furnace/Brew");
                        }
                        else
                        {
                            Click(Stage + "Furnace/Skip");
                        }

                        break;
                    case RunPhase.Brewing:
                        var cont = Active(Stage + "Furnace/Continue");
                        if (cont != null)
                            Click(cont);
                        break;
                    case RunPhase.Workshop:
                        var steel = BagButton("Thép");
                        if (steel != null && (_game.Selected == null || !_game.Selected.Def.IsBrewed))
                        {
                            Click(steel.gameObject);
                            var inspect = ActionButton("Soi bằng");
                            if (inspect != null)
                                Click(inspect.gameObject);
                        }

                        Click(Stage + "Workshop/Continue");
                        break;
                    case RunPhase.Offering:
                        var product = Find("Canvas/Game/Bag/Items/Container").GetComponentsInChildren<ItemButton>()
                            .First(b => b.Label.StartsWith("Thép") || b.Label.StartsWith("Gang"));
                        if (_game.Selected == null || !_game.Selected.Def.IsBrewed)
                            Click(product.gameObject);
                        Click(ActionButton("Dâng").gameObject);
                        break;
                    case RunPhase.Battle:
                        var battle = Active(Stage + "Battle/Continue");
                        if (battle != null)
                            Click(battle);
                        break;
                    case RunPhase.Spoils:
                        Click(Find(Stage + "Spoils/Choices/Container").transform.GetChild(0).gameObject);
                        break;
                }

                yield return null;
            }

            AssertNoLeak();
            Assert.That(phasesSeen, Is.SupersetOf(new[]
            {
                RunPhase.Decree, RunPhase.Market, RunPhase.Placing, RunPhase.Brewing, RunPhase.Workshop, RunPhase.Battle,
            }));
            Assert.That(Find(Stage + "End/Title").GetComponent<Text>().text, Is.Not.Empty);

            Click(Stage + "End/NewRun");
            Assert.That(_game.State.Season, Is.EqualTo(1));
            Assert.That(_game.State.Phase, Is.EqualTo(RunPhase.Decree));
            Assert.That(Active(Stage + "Decree"), Is.Not.Null);
        }
    }
}
