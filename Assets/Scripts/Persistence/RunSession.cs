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
        public const int Version = 1;

        private readonly ISaveStorage _storage;
        private readonly SaveFile _file;
        private int _snapshotSeason;
        private int _snapshotBrews;

        public RunStateMachine Machine { get; }

        private RunSession(RunStateMachine machine, SaveFile file, ISaveStorage storage)
        {
            Machine = machine;
            _file = file;
            _storage = storage;
            _snapshotSeason = file.snapshot.season;
            _snapshotBrews = file.snapshot.brewsUsed;
        }

        public static RunSession New(RunDef def, ulong seed, ISaveStorage storage, bool checkInvariants = false)
        {
            var machine = new RunStateMachine(def, seed, checkInvariants);
            var file = new SaveFile
            {
                version = Version,
                runDefId = def.Id,
                seed = seed.ToString(CultureInfo.InvariantCulture),
                snapshot = Snapshot.Capture(machine.State),
            };
            var session = new RunSession(machine, file, storage);
            session.Save();
            return session;
        }

        /// <summary>Khôi phục run đã lưu; null khi chưa có save, save khác version / RunDef, hoặc hỏng.</summary>
        public static RunSession Load(RunDef def, ISaveStorage storage, bool checkInvariants = false)
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
                var machine = new RunStateMachine(Snapshot.Restore(def, seed, file.snapshot), checkInvariants);
                for (var i = file.snapshotAt; i < file.actions.Count; i++)
                {
                    var command = CommandCodec.Decode(file.actions[i]);
                    if (command == null || !machine.Execute(command).Accepted)
                        throw new System.FormatException($"action {i} ({file.actions[i]?.type}) không replay được.");
                }

                return new RunSession(machine, file, storage);
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
