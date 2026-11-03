using System.Collections.Generic;

namespace Alchemy.Core
{
    /// <summary>Tiến trình giữ qua các run: Danh vọng, bậc khó đã mở, mở khóa theo từng khoáng. Không có vàng, Túi, Uy tín.</summary>
    public sealed class MetaState
    {
        public int Renown { get; internal set; }

        /// <summary>Số bậc khó đã mở (bậc đầu luôn mở).</summary>
        public int UnlockedDifficulties { get; internal set; } = 1;

        // Sort theo Ordinal để file meta và kết quả ổn định giữa các platform.
        internal readonly List<string> ItemList = new List<string>();
        internal readonly List<string> RecipeList = new List<string>();

        public IReadOnlyList<string> UnlockedItems => ItemList;
        public IReadOnlyList<string> UnlockedRecipes => RecipeList;

        public MetaState()
        {
        }

        internal MetaState(int renown, int unlockedDifficulties, IEnumerable<string> items, IEnumerable<string> recipes)
        {
            Renown = renown;
            UnlockedDifficulties = unlockedDifficulties < 1 ? 1 : unlockedDifficulties;
            foreach (var id in items)
                Add(ItemList, id);
            foreach (var id in recipes)
                Add(RecipeList, id);
        }

        public bool IsDifficultyUnlocked(RunDef def, DifficultyDef difficulty)
        {
            for (var i = 0; i < def.Difficulties.Count && i < UnlockedDifficulties; i++)
                if (def.Difficulties[i] == difficulty)
                    return true;
            return false;
        }

        internal static bool Add(List<string> list, string id)
        {
            var i = list.BinarySearch(id, System.StringComparer.Ordinal);
            if (i >= 0)
                return false;
            list.Insert(~i, id);
            return true;
        }
    }

    /// <summary>Những gì một run kết thúc mang về meta; Presentation hiện ở màn kết thúc.</summary>
    public sealed class RunRewards
    {
        public int RenownGained { get; internal set; }
        public DifficultyDef DifficultyUnlocked { get; internal set; }

        /// <summary>Tên những thứ vừa vượt mốc Danh vọng (bộ khởi đầu, hàng Chợ, chiến lợi phẩm).</summary>
        public List<string> Unlocks { get; } = new List<string>();
    }

    public static class MetaProgress
    {
        public static int RenownFor(RunState s)
        {
            var d = s.Def;
            long renown = (long)s.Wins * d.RenownPerWin + (long)s.HighestTier * d.RenownPerTier
                          + (s.Outcome == RunOutcome.Won ? d.RenownRunWin : 0);
            renown = renown * s.Difficulty.RenownPercent / 100;
            return (int)System.Math.Max(0, System.Math.Min(renown, int.MaxValue / 2));
        }

        /// <summary>Gộp kết quả run đã kết thúc vào meta. Nung ra → mở vật phẩm + công thức; mua / nhận → chỉ vật phẩm.</summary>
        public static RunRewards Apply(MetaState meta, RunState s)
        {
            if (s.Phase != RunPhase.RunEnd)
                throw new System.InvalidOperationException("Chỉ quy đổi Danh vọng khi run đã kết thúc.");

            var rewards = new RunRewards { RenownGained = RenownFor(s) };
            var before = meta.Renown;
            meta.Renown = before + rewards.RenownGained;

            foreach (var id in s.Brewed)
            {
                MetaState.Add(meta.ItemList, id);
                MetaState.Add(meta.RecipeList, id);
            }

            foreach (var id in s.Acquired)
                MetaState.Add(meta.ItemList, id);

            var index = IndexOf(s.Def.Difficulties, s.Difficulty);
            if (s.Outcome == RunOutcome.Won && index + 1 < s.Def.Difficulties.Count && meta.UnlockedDifficulties <= index + 1)
            {
                meta.UnlockedDifficulties = index + 2;
                rewards.DifficultyUnlocked = s.Def.Difficulties[index + 1];
            }

            bool Crossed(int threshold) => threshold > before && threshold <= meta.Renown;
            foreach (var kit in s.Def.StarterKits)
                if (Crossed(kit.UnlockRenown))
                    rewards.Unlocks.Add(kit.Name);
            foreach (var item in s.Def.ShopPool)
                if (Crossed(item.UnlockRenown) && !rewards.Unlocks.Contains(item.Name))
                    rewards.Unlocks.Add(item.Name);
            foreach (var r in s.Def.Rewards)
                if (Crossed(r.UnlockRenown))
                    rewards.Unlocks.Add(r.Name);
            return rewards;
        }

        private static int IndexOf<T>(IReadOnlyList<T> list, T x) where T : class
        {
            for (var i = 0; i < list.Count; i++)
                if (list[i] == x)
                    return i;
            return -1;
        }
    }

    /// <summary>Sổ công thức: chỉ nguyên liệu + thứ tự. Không lộ khoảng cách, phụ gia, RequiresMagic hay thuộc tính.</summary>
    public static class RecipeBook
    {
        public static IReadOnlyList<ItemDef> Ingredients(RunDef def, ItemDef product)
        {
            var list = new List<ItemDef>();
            if (product?.Recipe == null)
                return list;
            foreach (var slot in product.Recipe.Pattern)
                if (!slot.IsGap)
                    list.Add(def.FindItem(slot.ItemId));
            return list;
        }

        /// <summary>Thành phẩm có công thức đã mở (meta hoặc vừa nung trong run), theo thứ tự RunDef.Items.</summary>
        public static List<ItemDef> Entries(RunDef def, MetaState meta, RunState run)
        {
            var list = new List<ItemDef>();
            foreach (var item in def.Items)
                if (item.IsBrewed && (Has(meta?.UnlockedRecipes, item.Id) || Has(run?.Brewed, item.Id)))
                    list.Add(item);
            return list;
        }

        private static bool Has(IReadOnlyList<string> ids, string id)
        {
            if (ids == null)
                return false;
            foreach (var x in ids)
                if (x == id)
                    return true;
            return false;
        }
    }

    public static class RunSeed
    {
        /// <summary>Chuỗi số → chính số đó; chuỗi khác → FNV-1a của chuỗi (giống nhau mọi platform). Rỗng → null.</summary>
        public static ulong? Parse(string text)
        {
            text = text?.Trim();
            if (string.IsNullOrEmpty(text))
                return null;
            if (ulong.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var n))
                return n;
            var h = new StateHash();
            h.Add(text);
            return h.Value;
        }
    }
}
