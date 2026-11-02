using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Alchemy.Presentation
{
    /// <summary>Danh sách <see cref="ItemButton"/> dựng từ template, tái dùng qua ObjectPool.</summary>
    public class ButtonList : ViewBehaviour
    {
        [SerializeField, Tooltip("Template inactive, nằm ngoài container.")]
        private ItemButton _template;

        [SerializeField] private RectTransform _container;

        private readonly List<ItemButton> _active = new List<ItemButton>();
        private ObjectPool<ItemButton> _pool;

        protected override void CollectRequiredReferences(List<(string field, UnityEngine.Object value)> references)
        {
            references.Add((nameof(_template), _template));
            references.Add((nameof(_container), _container));
        }

        protected override void OnViewAwake()
        {
            _pool = new ObjectPool<ItemButton>(
                () => Instantiate(_template, _container),
                b =>
                {
                    b.gameObject.SetActive(true);
                    b.transform.SetAsLastSibling();
                },
                b => b.gameObject.SetActive(false),
                // Unity 6 Clear() mọi pool khi thoát Play mode, lúc nút đã bị hủy cùng scene.
                b =>
                {
                    if (b != null)
                        Destroy(b.gameObject);
                });
        }

        /// <summary>Hiện đúng <paramref name="count"/> nút; <paramref name="bind"/> gán nội dung từng nút.</summary>
        public void Show(int count, Action<ItemButton, int> bind)
        {
            if (_pool == null)
                return;
            foreach (var b in _active)
                _pool.Release(b);
            _active.Clear();

            for (var i = 0; i < count; i++)
            {
                var b = _pool.Get();
                _active.Add(b);
                bind(b, i);
            }
        }
    }
}
