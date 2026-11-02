using System.Collections.Generic;

namespace Alchemy.Core
{
    public enum BrewEventKind
    {
        Feed,
        Wait,
        Result,
    }

    public sealed class BrewEvent
    {
        public BrewEventKind Kind { get; }
        public int Tick { get; }

        /// <summary>Feed: item được nạp. Result: item thành phẩm (hoặc phế phẩm). Wait: null.</summary>
        public string ItemId { get; }

        public BrewEvent(BrewEventKind kind, int tick, string itemId)
        {
            Kind = kind;
            Tick = tick;
            ItemId = itemId;
        }
    }

    /// <summary>
    /// Thứ duy nhất Presentation nhận về một lượt nung: các tick từ nguyên liệu đầu tới nguyên liệu cuối
    /// rồi một sự kiện kết quả (I15). Không chứa khoảng cách công thức, slot sai hay thuộc tính.
    /// </summary>
    public sealed class BrewLog
    {
        public IReadOnlyList<BrewEvent> Events { get; }

        public BrewLog(IReadOnlyList<BrewEvent> events)
        {
            Events = events;
        }
    }

    public sealed class BrewResult
    {
        /// <summary>Null khi lưới trống lúc Nung.</summary>
        public Product Product { get; }
        public BrewLog Log { get; }

        public BrewResult(Product product, BrewLog log)
        {
            Product = product;
            Log = log;
        }
    }
}
