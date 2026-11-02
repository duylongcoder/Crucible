using System.Collections;
using Alchemy.Core;
using Alchemy.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Alchemy.Tests.PlayMode
{
    /// <summary>Smoke test: scene khởi động load được và hiển thị nhãn build.</summary>
    public class MainSceneTests
    {
        [UnityTest]
        public IEnumerator MainScene_Loads_BootViewShowsMilestone()
        {
            yield return SceneManager.LoadSceneAsync("Main");

            var view = Object.FindAnyObjectByType<BootView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.enabled, Is.True);
            // Canvas còn chứa UI greybox; nhắm đúng nhãn build.
            Assert.That(view.transform.Find("BuildLabel").GetComponent<Text>().text, Does.Contain(GameInfo.Milestone));
        }
    }
}
