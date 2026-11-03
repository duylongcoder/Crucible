using Alchemy.Core;
using UnityEngine;

namespace Alchemy.Persistence
{
    /// <summary>Meta (Danh vọng, bậc khó đã mở, mở khóa) trong file riêng, không bị xóa khi run kết thúc.</summary>
    public sealed class MetaStore
    {
        public const int Version = 1;

        private readonly ISaveStorage _storage;

        public MetaState State { get; }

        private MetaStore(MetaState state, ISaveStorage storage)
        {
            State = state;
            _storage = storage;
        }

        /// <summary>Chưa có file → meta mới. File hỏng → giữ nguyên file (không ghi đè mất tiến trình), dùng meta mới trong phiên này.</summary>
        public static MetaStore Load(ISaveStorage storage)
        {
            var text = storage.Read();
            if (string.IsNullOrEmpty(text))
                return new MetaStore(new MetaState(), storage);

            try
            {
                var f = JsonUtility.FromJson<MetaFile>(text);
                if (f == null || f.version != Version)
                    throw new System.FormatException($"meta version {f?.version}.");
                return new MetaStore(new MetaState(f.renown, f.unlockedDifficulties, f.items, f.recipes), storage);
            }
            catch (System.Exception e) when (e is System.ArgumentException || e is System.FormatException)
            {
                Debug.LogError($"Không đọc được meta, tiến trình phiên này sẽ không được lưu: {e.Message}");
                return new MetaStore(new MetaState(), null);
            }
        }

        public void Save()
        {
            if (_storage == null)
                return;
            var f = new MetaFile
            {
                version = Version,
                renown = State.Renown,
                unlockedDifficulties = State.UnlockedDifficulties,
            };
            f.items.AddRange(State.UnlockedItems);
            f.recipes.AddRange(State.UnlockedRecipes);
            try
            {
                _storage.Write(JsonUtility.ToJson(f));
            }
            catch (System.IO.IOException e)
            {
                Debug.LogException(e);
            }
        }
    }
}
