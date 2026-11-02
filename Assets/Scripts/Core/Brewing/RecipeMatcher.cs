using System.Collections.Generic;

namespace Alchemy.Core
{
    internal sealed class RecipeMatcher
    {
        private readonly List<ItemDef> _products = new List<ItemDef>();

        /// <summary>Nhận mọi item; chỉ giữ những item có <see cref="ItemDef.Recipe"/>.</summary>
        public RecipeMatcher(IEnumerable<ItemDef> items)
        {
            foreach (var item in items)
                if (item.Recipe != null)
                    _products.Add(item);
        }

        /// <summary>Thành phẩm khớp chuỗi nạp, hoặc null nếu không khớp công thức nào (phế phẩm).</summary>
        public ItemDef Match(IReadOnlyList<Product> feed)
        {
            ItemDef found = null;
            foreach (var item in _products)
            {
                if (!Matches(item.Recipe, feed))
                    continue;
                // I2: validator phải chặn từ content; tới được đây là lỗi content, không được chọn bừa.
                if (found != null)
                    throw new System.InvalidOperationException($"I2: chuỗi nạp khớp cả '{found.Id}' và '{item.Id}'.");
                found = item;
            }

            return found;
        }

        public static bool Matches(Recipe recipe, IReadOnlyList<Product> feed)
        {
            var start = 0;
            while (start < feed.Count && feed[start] == null)
                start++;

            var pattern = recipe.Pattern;
            if (start + pattern.Count > feed.Count)
                return false;

            for (var i = 0; i < pattern.Count; i++)
                if (!SlotAccepts(recipe, pattern[i], feed[start + i]))
                    return false;

            for (var i = start + pattern.Count; i < feed.Count; i++)
                if (feed[i] != null)
                    return false;

            return true;
        }

        private static bool SlotAccepts(Recipe recipe, RecipeSlot slot, Product cell)
        {
            if (slot.IsGap)
                return cell == null || (cell.Def.Kind == ItemKind.Additive && Contains(recipe.AllowedAdditives, cell.Def.Id));

            return cell != null
                && cell.Def.Kind != ItemKind.Scrap
                && cell.Def.Id == slot.ItemId
                && (!slot.RequiresMagic || cell.IsMagic);
        }

        private static bool Contains(IReadOnlyList<string> ids, string id)
        {
            foreach (var x in ids)
                if (x == id)
                    return true;
            return false;
        }
    }
}
