using System.Collections.Generic;

namespace Alchemy.Core
{
    /// <summary>Quality = sum(Essence) × ((1 + sum(AddPotency)) × product(MulPotency)), clamp theo I3.</summary>
    public static class ScoreCalculator
    {
        public const double MaxValue = 1e300;

        // Add trước, Mul sau, bất kể thứ tự kích hoạt: nhận danh sách đã tách nên thứ tự không đổi kết quả.
        public static double Potency(IReadOnlyList<double> addPotency, IReadOnlyList<double> mulPotency)
        {
            var potency = 1.0;
            foreach (var a in addPotency)
                potency += a;
            potency = Clamp(potency, 1);
            foreach (var m in mulPotency)
                potency = Clamp(potency * m, 1);
            return potency;
        }

        public static double Quality(IReadOnlyList<double> essence, IReadOnlyList<double> addPotency, IReadOnlyList<double> mulPotency)
        {
            var sum = 0.0;
            foreach (var e in essence)
                sum += e;
            return Clamp(Clamp(sum, 0) * Potency(addPotency, mulPotency), 0);
        }

        /// <summary>NaN về min; ±Infinity và giá trị vượt biên về [min, 1e300].</summary>
        public static double Clamp(double value, double min)
        {
            if (double.IsNaN(value) || value < min)
                return min;
            return value > MaxValue ? MaxValue : value;
        }
    }
}
