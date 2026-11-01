using System.Collections.Generic;
using System.Text.RegularExpressions;
using Alchemy.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Alchemy.Tests.PlayMode
{
    public class ViewBehaviourTests
    {
        private class FakeView : ViewBehaviour
        {
            public Object Target;
            public bool Initialized;

            protected override void CollectRequiredReferences(List<(string field, Object value)> references)
            {
                references.Add((nameof(Target), Target));
            }

            protected override void OnViewAwake()
            {
                Initialized = true;
            }
        }

        private GameObject _go;
        private GameObject _target;

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_go);
            Object.Destroy(_target);
        }

        [Test]
        public void Awake_WhenRequiredReferenceMissing_DisablesAndLogsError()
        {
            LogAssert.Expect(LogType.Error, new Regex("Target"));

            var view = CreateView(target: null);

            Assert.That(view.enabled, Is.False);
            Assert.That(view.Initialized, Is.False);
        }

        [Test]
        public void Awake_WhenReferencesAssigned_StaysEnabledAndInitializes()
        {
            _target = new GameObject("Target");
            var view = CreateView(_target);

            Assert.That(view.enabled, Is.True);
            Assert.That(view.Initialized, Is.True);
        }

        // Tạo inactive rồi mới bật để gán field trước khi Awake chạy.
        private FakeView CreateView(Object target)
        {
            _go = new GameObject("FakeView");
            _go.SetActive(false);
            var view = _go.AddComponent<FakeView>();
            view.Target = target;
            _go.SetActive(true);
            return view;
        }
    }
}
