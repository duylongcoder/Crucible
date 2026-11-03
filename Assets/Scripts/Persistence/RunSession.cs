using System.Globalization;
using Alchemy.Core;
using UnityEngine;

namespace Alchemy.Persistence
{
    /// <summary>
    /// Run đang chơi + file save của nó. Lưu sau mỗi Command được chấp nhận ("Dừng là dừng"):
    /// actions[] đầy đủ + snapshot đầu mỗi lượt; mở lại = snapshot gần nhất + replay phần actions sau nó.
    /// </summary>
    public sealed class RunSession
    {
        /// <summary>Tăng khi đổi định dạng save hoặc luật làm replay cũ sai; save khác version bị bỏ qua.</summary>
        public const int Version = 2;

        private readonly ISaveStorage _storage;
        private readonly MetaStore _meta;
        private readonly SaveFile _file;
        private int _snapshotSeason;
        private int _snapshotBrews;

        public RunStateMachine Machine { get; }

        /// <summary>Danh vọng và mở khóa run vừa mang về; có khi run kết thúc.</summary>
        public RunRewards Rewards { get; private set; }

        private RunSession(RunStateMachine machine, SaveFile file, ISaveStorage storage, MetaStore meta)
        {
            Machine = machine;
            _file = file;
            _storage = storage;
            _meta = meta;
            _snapshotSeason = file.snapshot.season;
            _snapshotBrews = file.snapshot.brewsUsed;
        }

        /// <param name="meta">Null = không ghi meta (test).</param>
        public static RunSession New(RunDef def, ulong seed, RunSetup setup, ISaveStorage storage, MetaStore meta = null,
            bool checkInvariants = false)
        {
            var machine = new RunStateMachine(def, seed, setup, checkInvariants);
            var s = machine.State;
            var file = new SaveFile
            {
                version = Version,
                runDefId = def.Id,
                seed = seed.ToString(CultureInfo.InvariantCulture),
                difficulty = s.Difficulty.Id,
                starterKit = s.StarterKit.Id,
                renown = s.Renown,
                snapshot = Snapshot.Capture(s),
            };
            var session = new RunSession(machine, file, storage, meta);
            session.Save();
            return session;
        }

        /// <summary>Khôi phục run đã lưu; null khi chưa có save, save khác version / RunDef, hoặc hỏng.</summary>
        public static RunSession Load(RunDef def, ISaveStorage storage, MetaStore meta = null, bool checkInvariants = false)
        {
            var text = storage.Read();
            if (string.IsNullOrEmpty(text))
                return null;

            try
            {
                var file = JsonUtility.FromJson<SaveFile>(text);
                if (file == null || file.version != Version || file.runDefId != def.Id || file.snapshot == null
                    || file.snapshotAt < 0 || file.snapshotAt > file.actions.Count)
                    throw new System.FormatException($"save version {file?.version}, RunDef '{file?.runDefId}'.");

                var seed = ulong.Parse(file.seed, CultureInfo.InvariantCulture);
                var difficulty = Snapshot.Find(def.Difficulties, file.difficulty, d => d.Id)
                                 ?? throw new System.FormatException("save thiếu bậc khó.");
                var kit = Snapshot.Find(def.StarterKits, file.starterKit, k => k.Id)
                          ?? throw new System.FormatException("save thiếu bộ khởi đầu.");
                var restored = Snapshot.Restore(def, seed, difficulty, kit, file.renown, file.snapshot);
                var machine = new RunStateMachine(restored, checkInvariants);
                for (var i = file.snapshotAt; i < file.actions.Count; i++)
                {
                    var command = CommandCodec.Decode(file.actions[i]);
                    if (command == null || !machine.Execute(command).Accepted)
                        throw new System.FormatException($"action {i} ({file.actions[i]?.type}) không replay được.");
                }

                return new RunSession(machine, file, storage, meta);
            }
            catch (System.Exception e) when (e is System.FormatException || e is System.ArgumentException
                                             || e is System.OverflowException || e is System.InvalidOperationException)
            {
                Debug.LogWarning($"Bỏ qua save không dùng được: {e.Message}");
                return null;
            }
        }

        public CommandResult Execute(ICommand command)
        {
            var result = Machine.Execute(command);
            if (!result.Accepted)
                return result;

            _file.actions.Add(CommandCodec.Encode(command));
            var s = Machine.State;
            if (s.Phase == RunPhase.RunEnd)
            {
                // Ghi meta trước rồi mới xóa save run: tắt đúng giữa hai bước thì chỉ có thể quy đổi lặp, không mất tiến trình.
                if (_meta != null)
                {
                    Rewards = MetaProgress.Apply(_meta.State, s);
                    _meta.Save();
                }

                _storage.Delete();
                return result;
            }

            // Đầu lượt mới (Chiếu chỉ hoặc Chợ của lượt kế): chụp snapshot để lần mở sau chỉ replay lượt đang chơi.
            if ((s.Phase == RunPhase.Decree || s.Phase == RunPhase.Market)
                && (s.Season != _snapshotSeason || s.BrewsUsed != _snapshotBrews))
            {
                _file.snapshot = Snapshot.Capture(s);
                _file.snapshotAt = _file.actions.Count;
                _snapshotSeason = s.Season;
                _snapshotBrews = s.BrewsUsed;
            }

            Save();
            return result;
        }

        private void Save()
        {
            try
            {
                _storage.Write(JsonUtility.ToJson(_file));
            }
            catch (System.IO.IOException e)
            {
                // Không lưu được thì vẫn cho chơi tiếp; lần Command sau ghi lại toàn bộ.
                Debug.LogException(e);
            }
        }
    }
}
