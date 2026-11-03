namespace Alchemy.Core
{
    /// <summary>
    /// Thông tin nhận diện build. Nằm ở Core để mọi assembly dùng chung mà không cần UnityEngine.
    /// </summary>
    public static class GameInfo
    {
        public const string ProductName = "Crucible";

        /// <summary>Milestone đang làm (CLAUDE.md mục 2). Đổi khi chuyển milestone.</summary>
        public const string Milestone = "M4";
    }
}
