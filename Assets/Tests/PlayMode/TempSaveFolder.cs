using System.IO;
using Alchemy.Persistence;

namespace Alchemy.Tests.PlayMode
{
    /// <summary>Trỏ save run + meta sang thư mục tạm rỗng cho mỗi test; không đụng save thật trong persistentDataPath.</summary>
    internal static class TempSaveFolder
    {
        public static void Use()
        {
            Release();
            var dir = Path.Combine(Path.GetTempPath(), "crucible-playmode-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            FileSaveStorage.Folder = dir;
        }

        public static void Release()
        {
            var dir = FileSaveStorage.Folder;
            FileSaveStorage.Folder = null;
            if (dir != null && Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }
}
