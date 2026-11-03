using System.Collections;
using Alchemy.Core;
using Alchemy.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Alchemy.Tests.PlayMode
{
    /// <summary>Smoke test: scene Main dựng đủ view, panel bật theo phase, Run mới, resume.</summary>
    public class GreyboxTests
    {
        [SetUp]
        public void SetUp() => TempSaveFolder.Use();

        [TearDown]
        public void TearDown() => TempSaveFolder.Release();

        /// <summary>Không có save → panel Run mới; bấm Bắt đầu với bậc khó / bộ mặc định.</summary>
        private static GameController StartDefaultRun(string seed = "")
        {
            var game = Object.FindAnyObjectByType<GameController>();
            Assert.That(game, Is.Not.Null);
            Assert.That(game.enabled, Is.True, "GameController thiếu reference");
            Assert.That(game.HasRun, Is.False);
            Assert.That(Object.FindAnyObjectByType<SetupView>(), Is.Not.Null, "Không có save thì phải hiện panel Run mới");
            game.StartRun(game.Def.Difficulties[0], game.Def.StarterKits[0], seed);
            Assert.That(Object.FindAnyObjectByType<SetupView>(), Is.Null);
            return game;
        }

        [UnityTest]
        public IEnumerator MainScene_StartsRunAtDecree_ContinueShowsMarket()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;

            var game = StartDefaultRun();
            Assert.That(game.State.Phase, Is.EqualTo(RunPhase.Decree));
            Assert.That(Object.FindAnyObjectByType<DecreeView>(), Is.Not.Null);

            Assert.That(game.Send(new ContinueCommand()), Is.True);

            Assert.That(Object.FindAnyObjectByType<DecreeView>(), Is.Null, "Decree phải ẩn ở Chợ");
            var market = Object.FindAnyObjectByType<MarketView>();
            Assert.That(market, Is.Not.Null);
            Assert.That(market.enabled, Is.True, "MarketView thiếu reference");
        }

        [UnityTest]
        public IEnumerator MainScene_Reload_ResumesSavedRunAtSamePhase()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            var game = StartDefaultRun("12345");
            Assert.That(game.State.Seed, Is.EqualTo(12345UL));
            game.Send(new ContinueCommand());
            game.Send(new LeaveMarketCommand());
            var hash = game.State.ComputeHash();

            // Như đóng tab rồi mở lại: scene mới, GameController mới đọc save.
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            var resumed = Object.FindAnyObjectByType<GameController>();

            Assert.That(resumed, Is.Not.SameAs(game));
            Assert.That(resumed.State.Phase, Is.EqualTo(RunPhase.Placing));
            Assert.That(resumed.State.ComputeHash(), Is.EqualTo(hash));
            Assert.That(Object.FindAnyObjectByType<SetupView>(), Is.Null, "Có run dở thì không hiện panel Run mới");
        }

        [UnityTest]
        public IEnumerator Hud_BookButton_OpensRecipeBookWithoutGaps()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            var game = StartDefaultRun();

            game.OpenBook();

            var book = Object.FindAnyObjectByType<RecipeBookView>();
            Assert.That(book, Is.Not.Null);
            var text = book.GetComponentInChildren<UnityEngine.UI.Text>(true).transform.parent
                .GetComponentsInChildren<UnityEngine.UI.Text>(true);
            foreach (var t in text)
                Assert.That(t.text, Does.Not.Contain("Gap").And.Not.Contain("khoảng trống"));
        }

        [UnityTest]
        public IEnumerator Hud_BookButton_DisabledWhileBrewing_NoEarlyResult()
        {
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            var game = StartDefaultRun();
            int Index(string id)
            {
                for (var i = 0; i < game.State.Bag.Count; i++)
                    if (game.State.Bag[i].Def.Id == id)
                        return i;
                return -1;
            }

            game.Send(new ContinueCommand());
            game.Send(new LeaveMarketCommand());
            game.Send(new PlaceCommand(Index("iron_ore"), 0, 0));
            game.Send(new PlaceCommand(Index("coal"), 0, 2));
            game.Send(new BrewCommand());
            Assert.That(game.State.Phase, Is.EqualTo(RunPhase.Brewing));

            var book = GameObject.Find("Canvas/Game/Hud/Book").GetComponent<UnityEngine.UI.Button>();
            Assert.That(book.interactable, Is.False, "Mở Sổ lúc đang nung sẽ lộ thành phẩm trước tick cuối");
        }

        [UnityTest]
        public IEnumerator MainScene_AllViewsHaveReferences()
        {
            yield return SceneManager.LoadSceneAsync("Main");

            foreach (var view in Object.FindObjectsByType<ViewBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // View inactive chưa chạy Awake; bật lên để ViewBehaviour kiểm reference.
                var go = view.gameObject;
                var wasActive = go.activeSelf;
                go.SetActive(true);
                Assert.That(view.enabled, Is.True, $"{view.GetType().Name} trên {go.name} thiếu reference");
                go.SetActive(wasActive);
            }
        }
    }
}
