using System.Collections.Generic;
using System.Text;
using Alchemy.Core;
using UnityEngine;

namespace Alchemy.Presentation
{
    /// <summary>Sổ công thức: nguyên liệu + thứ tự của công thức đã mở. Không bao giờ có khoảng cách.</summary>
    public class RecipeBookView : ViewBehaviour
    {
        [SerializeField] private GameController _game;
        [SerializeField] private UnityEngine.UI.Text _body;
        [SerializeField] private UnityEngine.UI.Button _close;

        protected override void CollectRequiredReferences(List<(string field, Object value)> references)
        {
            references.Add((nameof(_game), _game));
            references.Add((nameof(_body), _body));
            references.Add((nameof(_close), _close));
        }

        protected override void OnViewAwake() => _close.onClick.AddListener(() => gameObject.SetActive(false));

        public void Open()
        {
            gameObject.SetActive(true);
            var def = _game.Def;
            var entries = RecipeBook.Entries(def, _game.Meta.State, _game.HasRun ? _game.State : null);
            if (entries.Count == 0)
            {
                _body.text = "Chưa có công thức nào.\nNung ra một thành phẩm để ghi vào Sổ.";
                return;
            }

            var sb = new StringBuilder();
            foreach (var item in entries)
            {
                sb.Append("• ").Append(item.Name).Append(": ");
                var ingredients = RecipeBook.Ingredients(def, item);
                for (var i = 0; i < ingredients.Count; i++)
                    sb.Append(i == 0 ? "" : " → ").Append(ingredients[i].Name);
                sb.Append('\n');
            }

            sb.Append("\n<color=#a89c85>Chỉ ghi nguyên liệu và thứ tự. Khoảng cách phải tự tìm.</color>");
            _body.text = sb.ToString();
        }
    }
}
