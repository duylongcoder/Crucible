using System.Collections.Generic;

namespace Alchemy.Core
{
    public static class StatIds
    {
        public const string Quality = "quality";
        public const string Magic = "magic";
    }

    public sealed class StatLine
    {
        public string StatId { get; }
        public double Value { get; }

        public StatLine(string statId, double value)
        {
            StatId = statId;
            Value = value;
        }
    }

    /// <summary>
    /// Instance vật phẩm trong Túi / trên lưới. Thành phẩm nung ra mang các dòng thuộc tính ẩn;
    /// vật phẩm gốc (mua, khởi đầu) và phế phẩm không có dòng nào.
    /// </summary>
    public sealed class Product
    {
        public ItemDef Def { get; }
        public double Essence { get; }
        public double Potency { get; }
        public IReadOnlyList<StatLine> Stats { get; }

        public Product(ItemDef def, double essence = 0, double potency = 1, IReadOnlyList<StatLine> stats = null)
        {
            Def = def;
            Essence = essence;
            Potency = potency;
            Stats = stats ?? System.Array.Empty<StatLine>();
        }

        public bool IsMagic => HasStat(StatIds.Magic);

        public double Quality
        {
            get
            {
                foreach (var s in Stats)
                    if (s.StatId == StatIds.Quality)
                        return s.Value;
                return 0;
            }
        }

        public bool HasStat(string statId)
        {
            foreach (var s in Stats)
                if (s.StatId == statId)
                    return true;
            return false;
        }

        public void AddTo(StateHash hash)
        {
            hash.Add(Def.Id);
            hash.Add(Essence);
            hash.Add(Potency);
            hash.Add(Stats.Count);
            foreach (var s in Stats)
            {
                hash.Add(s.StatId);
                hash.Add(s.Value);
            }
        }
    }
}
