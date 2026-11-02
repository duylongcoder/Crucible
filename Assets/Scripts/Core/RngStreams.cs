namespace Alchemy.Core
{
    /// <summary>Mỗi hệ thống một stream (I6) để random của hệ thống này không làm lệch hệ thống khác.</summary>
    public enum RngStream
    {
        Shop,
        Chance,
        Inspect,
        Forge,
        Market,
        Season,
        Reward,
    }

    public sealed class RngStreams
    {
        private readonly Pcg32[] _streams;

        public RngStreams(ulong seed)
        {
            var count = System.Enum.GetValues(typeof(RngStream)).Length;
            _streams = new Pcg32[count];
            for (var i = 0; i < count; i++)
                _streams[i] = new Pcg32(seed, (ulong)i);
        }

        public Pcg32 this[RngStream stream] => _streams[(int)stream];

        public void AddTo(StateHash hash)
        {
            foreach (var s in _streams)
            {
                hash.Add(s.State);
                hash.Add(s.Increment);
            }
        }
    }
}
