namespace Alchemy.Core
{
    /// <summary>
    /// Lưới chỉ là dòng thời gian viết gọn: vị trí không gian không có ý nghĩa, chỉ thứ tự quét hàng.
    /// </summary>
    public sealed class Grid
    {
        private readonly Product[] _cells;

        public int Width { get; }
        public int Height { get; }

        public Grid(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new Product[width * height];
        }

        public Product this[int row, int col] => _cells[Index(row, col)];

        /// <summary>Chỉ đặt vào ô trống (I11). Phế phẩm không đặt được (I13).</summary>
        public bool Place(int row, int col, Product product)
        {
            var i = Index(row, col);
            if (product == null || product.Def.Kind == ItemKind.Scrap || _cells[i] != null)
                return false;
            _cells[i] = product;
            return true;
        }

        /// <summary>Gỡ khỏi ô, trả về vật phẩm (null nếu ô trống).</summary>
        public Product Remove(int row, int col)
        {
            var i = Index(row, col);
            var product = _cells[i];
            _cells[i] = null;
            return product;
        }

        /// <summary>Chuỗi nạp: trái → phải, trên → dưới. Phần tử i ứng với tick i; null = tick chờ.</summary>
        public Product[] ToFeedSequence() => (Product[])_cells.Clone();

        private int Index(int row, int col)
        {
            if (row < 0 || row >= Height || col < 0 || col >= Width)
                throw new System.ArgumentOutOfRangeException(nameof(row), $"Ô ({row},{col}) nằm ngoài lưới {Width}×{Height}.");
            return row * Width + col;
        }
    }
}
