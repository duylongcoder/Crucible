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
    /// Presentation chỉ thấy <see cref="Def"/> và <see cref="RevealedStats"/>.
    /// </summary>
    public sealed class Product
    {
        private readonly bool[] _revealed;

        public ItemDef Def { get; }
        internal double Essence { get; }
        internal double Potency { get; }
        internal IReadOnlyList<StatLine> Stats { get; }

        public Product(ItemDef def, double essence = 0, double potency = 1, IReadOnlyList<StatLine> stats = null)
        {
            Def = def;
            Essence = essence;
            Potency = potency;
            Stats = stats ?? System.Array.Empty<StatLine>();
            _revealed = new bool[Stats.Count];
        }

        internal bool IsMagic => HasStat(StatIds.Magic);

        internal double Quality
        {
            get
            {
                foreach (var s in Stats)
                    if (s.StatId == StatIds.Quality)
                        return s.Value;
                return 0;
            }
        }

        /// <summary>Các dòng đã soi bằng Kính soi, theo thứ tự dòng.</summary>
        public IReadOnlyList<StatLine> RevealedStats
        {
            get
            {
                var list = new List<StatLine>();
                for (var i = 0; i < Stats.Count; i++)
                    if (_revealed[i])
                        list.Add(Stats[i]);
                return list;
            }
        }

        public bool CanInspect => Def.IsBrewed && RevealedCount < Stats.Count;

        internal int RevealedCount
        {
            get
            {
                var n = 0;
                foreach (var r in _revealed)
                    if (r)
                        n++;
                return n;
            }
        }

        internal bool IsRevealed(int line) => _revealed[line];

        internal void Reveal(int line) => _revealed[line] = true;

        internal bool HasStat(string statId)
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
            for (var i = 0; i < Stats.Count; i++)
            {
                hash.Add(Stats[i].StatId);
                hash.Add(Stats[i].Value);
                hash.Add(_revealed[i]);
            }
        }
    }
}
