using System.IO;
using UnityEngine;

namespace Alchemy.Persistence
{
    public interface ISaveStorage
    {
        /// <summary>Null khi chưa có save.</summary>
        string Read();
        void Write(string text);
        void Delete();
    }

    /// <summary>
    /// Standalone: ghi file tạm rồi thay file cũ (tắt giữa chừng vẫn còn bản cũ nguyên vẹn).
    /// WebGL: persistentDataPath là IDBFS, không dựa vào atomic rename; ghi thẳng rồi flush sang IndexedDB.
    /// </summary>
    public sealed class FileSaveStorage : ISaveStorage
    {
        private readonly string _path;

        public FileSaveStorage(string path) => _path = path;

        /// <summary>Thư mục save; null = Application.persistentDataPath. Test PlayMode trỏ sang thư mục tạm để không đụng save thật.</summary>
        public static string Folder { get; set; }

        /// <summary>Save run đang chơi.</summary>
        public static FileSaveStorage Run => new FileSaveStorage(Path.Combine(Folder ?? Application.persistentDataPath, "run.json"));

        /// <summary>Meta giữ qua các run.</summary>
        public static FileSaveStorage Meta => new FileSaveStorage(Path.Combine(Folder ?? Application.persistentDataPath, "meta.json"));

        public string Read() => File.Exists(_path) ? File.ReadAllText(_path) : null;

        public void Write(string text)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            File.WriteAllText(_path, text);
            Flush();
#else
            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, text);
            if (File.Exists(_path))
                File.Replace(tmp, _path, null);
            else
                File.Move(tmp, _path);
#endif
        }

        public void Delete()
        {
            if (!File.Exists(_path))
                return;
            File.Delete(_path);
#if UNITY_WEBGL && !UNITY_EDITOR
            Flush();
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        // shortcut: JS_FileSystem_Sync có sẵn trong Unity 6.6 nhưng đã deprecated; khi Unity gỡ nó, chuyển sang
        // config.autoSyncPersistentDataPath = true trong WebGL template riêng.
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void JS_FileSystem_Sync();

        private static void Flush() => JS_FileSystem_Sync();
#endif
    }
}
