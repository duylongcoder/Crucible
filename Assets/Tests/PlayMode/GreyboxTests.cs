using System.Collections;
using Alchemy.Core;
using Alchemy.Persistence;
using Alchemy.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Alchemy.Tests.PlayMode
{
    /// <summary>Smoke test M3: scene Main dựng đủ view, panel bật theo phase.</summary>
    public class GreyboxTests
    {
        [UnityTest]
        public IEnumerator MainScene_StartsRunAtDecree_ContinueShowsMarket()
        {
            FileSaveStorage.Run.Delete();
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;

            var game = Object.FindAnyObjectByType<GameController>();
            Assert.That(game, Is.Not.Null);
            Assert.That(game.enabled, Is.True, "GameController thiếu reference");
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
            FileSaveStorage.Run.Delete();
            yield return SceneManager.LoadSceneAsync("Main");
            yield return null;
            var game = Object.FindAnyObjectByType<GameController>();
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
            FileSaveStorage.Run.Delete();
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
