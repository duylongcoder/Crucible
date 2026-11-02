namespace Alchemy.Core
{
    /// <summary>FNV-1a 64-bit. Không dùng GetHashCode vì nó khác nhau giữa các process/platform.</summary>
    public sealed class StateHash
    {
        private const ulong Prime = 1099511628211UL;

        public ulong Value { get; private set; } = 14695981039346656037UL;

        public void Add(ulong v)
        {
            for (var i = 0; i < 8; i++)
            {
                Value ^= (v >> (i * 8)) & 0xFF;
                Value = unchecked(Value * Prime);
            }
        }

        public void Add(int v) => Add(unchecked((ulong)v));

        public void Add(bool v) => Add(v ? 1UL : 0UL);

        public void Add(double v) => Add(unchecked((ulong)System.BitConverter.DoubleToInt64Bits(v)));

        public void Add(string s)
        {
            if (s == null)
            {
                Add(-1);
                return;
            }

            Add(s.Length);
            foreach (var c in s)
                Add((ulong)c);
        }
    }
}
