using System.Collections.Generic;

namespace Alchemy.Core
{
    /// <summary>Kính soi: lộ tối đa RevealLines dòng, chỉ trong các dòng chưa lộ (I17).</summary>
    internal static class Inspector
    {
        public static void Reveal(Product target, int revealLines, Pcg32 rng)
        {
            var hidden = new List<int>();
            for (var i = 0; i < target.Stats.Count; i++)
                if (!target.IsRevealed(i))
                    hidden.Add(i);

            if (hidden.Count <= revealLines)
            {
                foreach (var i in hidden)
                    target.Reveal(i);
                return;
            }

            // Fisher–Yates từng phần: chỉ xáo revealLines phần tử đầu.
            for (var k = 0; k < revealLines; k++)
            {
                var j = k + rng.NextInt(hidden.Count - k);
                (hidden[k], hidden[j]) = (hidden[j], hidden[k]);
                target.Reveal(hidden[k]);
            }
        }
    }
}
