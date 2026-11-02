namespace Alchemy.Core
{
    /// <summary>
    /// PCG32 (XSH-RR, pcg-random.org). Chỉ dùng phép toán số nguyên nên cho cùng kết quả trên mọi platform, kể cả WebGL.
    /// </summary>
    public sealed class Pcg32
    {
        private const ulong Multiplier = 6364136223846793005UL;

        public ulong State { get; private set; }
        public ulong Increment { get; }

        public Pcg32(ulong seed, ulong stream)
        {
            Increment = (stream << 1) | 1UL;
            NextUInt();
            State += seed;
            NextUInt();
        }

        public uint NextUInt()
        {
            var old = State;
            State = unchecked(old * Multiplier + Increment);
            var xorShifted = (uint)(((old >> 18) ^ old) >> 27);
            var rot = (int)(old >> 59);
            return (xorShifted >> rot) | (xorShifted << (-rot & 31));
        }

        /// <summary>Số nguyên đều trong [0, bound). Loại bỏ phần dư để không lệch phân phối.</summary>
        public int NextInt(int bound)
        {
            if (bound <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(bound));

            var b = (uint)bound;
            var threshold = unchecked(0u - b) % b;
            while (true)
            {
                var r = NextUInt();
                if (r >= threshold)
                    return (int)(r % b);
            }
        }
    }
}
