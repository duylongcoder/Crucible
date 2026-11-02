namespace Alchemy.Core
{
    /// <summary>Tham số luật nung. Giá trị đọc từ config của Content, không hard-code trong Core.</summary>
    public sealed class BrewRules
    {
        public int GridWidth { get; }
        public int GridHeight { get; }
        public int MagicCapPercent { get; }

        /// <summary>Truyền thừa: mỗi nguyên liệu đã kích hoạt ma thuật góp Tier × giá trị này (%).</summary>
        public int MagicPercentPerTier { get; }

        public int CellCount => GridWidth * GridHeight;

        public BrewRules(int gridWidth, int gridHeight, int magicCapPercent, int magicPercentPerTier)
        {
            if (gridWidth < 1 || gridHeight < 1 || gridWidth * gridHeight < 4)
                throw new System.ArgumentException("Lưới phải có ít nhất 4 ô (I8).");
            if (magicCapPercent < 0 || magicCapPercent > 100)
                throw new System.ArgumentOutOfRangeException(nameof(magicCapPercent));

            GridWidth = gridWidth;
            GridHeight = gridHeight;
            MagicCapPercent = magicCapPercent;
            MagicPercentPerTier = magicPercentPerTier;
        }
    }
}
