using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Alchemy.Presentation
{
    /// <summary>
    /// Base cho mọi MonoBehaviour của Presentation.
    /// Kiểm tra các reference bắt buộc một lần ở Awake: thiếu thì log lỗi chỉ rõ field và tự tắt,
    /// thay vì để NullReferenceException xảy ra muộn ở nơi khác.
    /// </summary>
    public abstract class ViewBehaviour : MonoBehaviour
    {
        /// <summary>Khai báo các field bắt buộc phải gán trong Inspector.</summary>
        protected abstract void CollectRequiredReferences(List<(string field, Object value)> references);

        /// <summary>Thay cho Awake ở lớp con; chỉ chạy khi đủ reference.</summary>
        protected virtual void OnViewAwake() { }

        // protected (không virtual) để lớp con khai báo Awake riêng sẽ bị compiler cảnh báo che khuất.
        protected void Awake()
        {
            var references = new List<(string field, Object value)>();
            CollectRequiredReferences(references);

            StringBuilder missing = null;
            foreach (var (field, value) in references)
            {
                if (value != null)
                    continue;
                missing ??= new StringBuilder();
                missing.Append(missing.Length == 0 ? field : ", " + field);
            }

            if (missing != null)
            {
                Debug.LogError($"{GetType().Name} trên '{name}' thiếu reference: {missing}. Đã tắt component.", this);
                enabled = false;
                return;
            }

            OnViewAwake();
        }
    }
}
