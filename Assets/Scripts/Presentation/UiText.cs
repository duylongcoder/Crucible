using System.Text;
using Alchemy.Core;
using UnityEngine;

namespace Alchemy.Presentation
{
    /// <summary>Chuỗi hiển thị và màu greybox. Chỉ dùng thông tin người chơi được phép biết.</summary>
    // shortcut: chuỗi giao diện viết trong code (tên vật phẩm, thuộc tính lấy từ data); chuyển sang localization khi cần đa ngôn ngữ.
    public static class UiText
    {
        public static readonly Color Panel = new Color(0.16f, 0.14f, 0.12f, 0.95f);
        public static readonly Color Slot = new Color(0.30f, 0.27f, 0.23f, 1f);
        public static readonly Color SlotEmpty = new Color(0.22f, 0.20f, 0.18f, 1f);
        public static readonly Color Selected = new Color(0.85f, 0.62f, 0.25f, 1f);
        public static readonly Color Active = new Color(0.95f, 0.45f, 0.15f, 1f);
        public static readonly Color Brewed = new Color(0.35f, 0.40f, 0.30f, 1f);
        public static readonly Color ScrapColor = new Color(0.35f, 0.25f, 0.25f, 1f);
        public static readonly Color TextColor = new Color(0.95f, 0.92f, 0.85f, 1f);

        public static string Phase(RunPhase phase)
        {
            switch (phase)
            {
                case RunPhase.Decree: return "Chiếu chỉ";
                case RunPhase.Market: return "Chợ";
                case RunPhase.Placing: return "Lò nung";
                case RunPhase.Brewing: return "Đang nung";
                case RunPhase.Workshop: return "Xưởng";
                case RunPhase.Offering: return "Dâng";
                case RunPhase.Battle: return "Rèn & Trận";
                case RunPhase.Spoils: return "Chiến lợi phẩm";
                default: return "Kết thúc";
            }
        }

        public static string Battle(BattleKind kind)
        {
            switch (kind)
            {
                case BattleKind.Tournament: return "Thi đấu";
                case BattleKind.War: return "Chiến tranh";
                default: return "Đại chiến";
            }
        }

        public static string Kind(ItemKind kind)
        {
            switch (kind)
            {
                case ItemKind.Mineral: return "Khoáng";
                case ItemKind.Additive: return "Phụ gia";
                case ItemKind.Lens: return "Kính soi";
                case ItemKind.Catalyst: return "Xúc tác";
                case ItemKind.Reagent: return "Thuốc thử";
                default: return "Phế phẩm";
            }
        }

        public static string Stars(int stars) => new string('★', stars) + new string('☆', Mathf.Max(0, 5 - stars));

        public static string Item(ItemDef def) =>
            def.Kind == ItemKind.Scrap ? def.Name : $"{def.Name}\n<size=16>{Kind(def.Kind)} · cấp {def.Tier}</size>";

        public static Color ItemColor(ItemDef def) =>
            def.Kind == ItemKind.Scrap ? ScrapColor : def.IsBrewed ? Brewed : Slot;

        public static string Modifier(SeasonModifierDef m) => m == null ? "không có" : $"{m.Name} — {m.Description}";

        public static string Reward(RewardDef r)
        {
            switch (r.Kind)
            {
                case RewardKind.BagExpand: return $"{r.Name}\n<size=16>+{r.Amount} chỗ trong Túi</size>";
                case RewardKind.Catalyst: return $"{r.Name}\n<size=16>{r.Item.Description}</size>";
                default: return $"{r.Name}\n<size=16>Nhận 1 {r.Item.Name} (cấp {r.Item.Tier})</size>";
            }
        }

        /// <summary>Chi tiết vật phẩm: chỉ các dòng thuộc tính đã soi.</summary>
        public static string Detail(Product p, System.Func<string, string> statName)
        {
            var sb = new StringBuilder();
            sb.Append(Kind(p.Def.Kind)).Append(" · cấp ").Append(p.Def.Tier);
            if (p.Def.Description.Length > 0)
                sb.Append('\n').Append(p.Def.Description);
            if (p.Def.Kind == ItemKind.Lens)
                sb.Append("\nSoi tối đa ").Append(p.Def.RevealLines).Append(" dòng thuộc tính.");
            if (!p.Def.IsBrewed)
                return sb.ToString();

            var revealed = p.RevealedStats;
            sb.Append(revealed.Count == 0 ? "\nChưa soi." : "\nĐã soi:");
            foreach (var line in revealed)
            {
                sb.Append("\n• ").Append(statName(line.StatId));
                if (line.StatId != StatIds.Magic)
                    sb.Append(": ").Append(line.Value.ToString("0.#"));
            }

            return sb.ToString();
        }
    }
}
