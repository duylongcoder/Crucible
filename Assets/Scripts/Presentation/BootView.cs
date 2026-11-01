using System.Collections.Generic;
using Alchemy.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Alchemy.Presentation
{
    /// <summary>
    /// View của scene Main ở M0: hiển thị nhãn build để xác nhận build (WebGL) khởi động được.
    /// </summary>
    public class BootView : ViewBehaviour
    {
        [SerializeField, Tooltip("Text hiển thị tên sản phẩm, milestone và phiên bản build.")]
        private Text _buildLabel;

        protected override void CollectRequiredReferences(List<(string field, Object value)> references)
        {
            references.Add((nameof(_buildLabel), _buildLabel));
        }

        protected override void OnViewAwake()
        {
            _buildLabel.text = $"{GameInfo.ProductName} · {GameInfo.Milestone} · v{Application.version}";
        }
    }
}
