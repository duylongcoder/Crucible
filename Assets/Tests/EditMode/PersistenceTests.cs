using System.Collections.Generic;
using System.IO;
using Alchemy.Core;
using Alchemy.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Alchemy.Tests.EditMode
{
    /// <summary>M4: tắt giữa lượt, mở lại đúng trạng thái ở bất kỳ phase nào (save = actions[] + snapshot đầu lượt).</summary>
    public class PersistenceTests
    {
        private sealed class MemoryStorage : ISaveStorage
        {
            public string Text;
            public string Read() => Text;
            public void Write(string text) => Text = text;
            public void Delete() => Text = null;
        }

        [Test]
        public void Session_ReloadAfterEveryCommand_SameHash_AllPhasesCovered()
        {
            var def = TestContent.MvpRun();
            var phases = new HashSet<RunPhase>();
            for (ulong seed = 0; seed < 15; seed++)
            {
                var storage = new MemoryStorage();
                var session = RunSession.New(def, seed, storage, checkInvariants: true);
                var agent = new System.Random((int)seed);
                for (var i = 0; i < 3000 && session.Machine.State.Phase != RunPhase.RunEnd; i++)
                {
                    // Seed lẻ dùng agent theo kịch bản để có trận Thắng và Chiến lợi phẩm.
                    var s = session.Machine.State;
                    var command = seed % 2 == 1 ? RandomAgentTests.Scripted(s) : RandomAgentTests.NextCommand(s, agent);
                    if (!session.Execute(command).Accepted
                        || session.Machine.State.Phase == RunPhase.RunEnd)
                        continue;

                    var live = session.Machine.State;
                    phases.Add(live.Phase);
                    var loaded = RunSession.Load(def, storage, checkInvariants: true);
                    Assert.That(loaded, Is.Not.Null, $"seed {seed}, command {i}");
                    Assert.That(loaded.Machine.State.ComputeHash(), Is.EqualTo(live.ComputeHash()), $"seed {seed}, command {i}, {live.Phase}");
                    // Tiếp tục chơi trên bản vừa mở lại, như người chơi đóng tab rồi mở lại.
                    session = loaded;
                }
            }

            foreach (RunPhase phase in System.Enum.GetValues(typeof(RunPhase)))
                if (phase != RunPhase.RunEnd)
                    Assert.That(phases, Does.Contain(phase));
        }

        [Test]
        public void Session_Reload_InBrewingAndBattle_KeepsResultForReplay()
        {
            var def = TestContent.MvpRun();
            var storage = new MemoryStorage();
            var session = RunSession.New(def, 5, storage);
            session.Execute(new ContinueCommand());
            session.Execute(new LeaveMarketCommand());
            session.Execute(new PlaceCommand(0, 0, 0));
            session.Execute(new BrewCommand());

            var loaded = RunSession.Load(def, storage);

            Assert.That(loaded.Machine.State.Phase, Is.EqualTo(RunPhase.Brewing));
            Assert.That(loaded.Machine.State.LastBrew.Log.Events.Count,
                Is.EqualTo(session.Machine.State.LastBrew.Log.Events.Count));
        }

        [Test]
        public void Session_RunEnd_DeletesSave()
        {
            var def = TestContent.MvpRun();
            var storage = new MemoryStorage();
            var session = RunSession.New(def, 1, storage);
            // Không dâng gì mỗi Mùa → thua 3 trận → trục xuất.
            for (var i = 0; i < 200 && session.Machine.State.Phase != RunPhase.RunEnd; i++)
                session.Execute(session.Machine.State.Phase == RunPhase.Market || session.Machine.State.Phase == RunPhase.Placing
                    ? new SkipBrewCommand()
                    : session.Machine.State.Phase == RunPhase.Offering ? (ICommand)new OfferCommand(0) : new ContinueCommand());

            Assert.That(session.Machine.State.Phase, Is.EqualTo(RunPhase.RunEnd));
            Assert.That(storage.Text, Is.Null);
            Assert.That(RunSession.Load(def, storage), Is.Null);
        }

        [Test]
        public void Session_SnapshotAdvancesEachTurn()
        {
            var def = TestContent.MvpRun();
            var storage = new MemoryStorage();
            var session = RunSession.New(def, 2, storage);
            session.Execute(new ContinueCommand());
            session.Execute(new SkipBrewCommand());

            var file = JsonUtility.FromJson<SaveFile>(storage.Text);

            Assert.That(file.actions.Count, Is.EqualTo(2));
            Assert.That(file.snapshotAt, Is.EqualTo(2), "Chợ lượt 2 là đầu lượt mới");
            Assert.That(file.snapshot.brewsUsed, Is.EqualTo(1));
        }

        [TestCase("{ not json")]
        [TestCase("{\"version\":999,\"runDefId\":\"mvp\"}")]
        [TestCase("{\"version\":1,\"runDefId\":\"other\"}")]
        public void Load_WhenSaveUnusable_ReturnsNull(string text)
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Bỏ qua save"));

            Assert.That(RunSession.Load(TestContent.MvpRun(), new MemoryStorage { Text = text }), Is.Null);
        }

        [Test]
        public void Load_WhenActionDoesNotReplay_ReturnsNull()
        {
            var def = TestContent.MvpRun();
            var storage = new MemoryStorage();
            RunSession.New(def, 1, storage).Execute(new ContinueCommand());
            storage.Text = storage.Text.Replace("\"continue\"", "\"brew\"");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Bỏ qua save"));

            Assert.That(RunSession.Load(def, storage), Is.Null);
        }

        [Test]
        public void Load_WhenNoSave_ReturnsNull()
        {
            Assert.That(RunSession.Load(TestContent.MvpRun(), new MemoryStorage()), Is.Null);
        }

        private static IEnumerable<ICommand> AllCommands() => new ICommand[]
        {
            new ContinueCommand(), new BuyCommand(1), new SellCommand(2), new RerollCommand(), new LeaveMarketCommand(),
            new PlaceCommand(3, 1, 2), new UnplaceCommand(2, 1), new MoveCommand(0, 1, 2, 0), new BrewCommand(),
            new SkipBrewCommand(), new InspectCommand(4, 5), new OfferCommand(6), new ChooseRewardCommand(2),
        };

        [TestCaseSource(nameof(AllCommands))]
        public void CommandCodec_RoundTrip_SameTypeAndArgs(ICommand command)
        {
            var decoded = CommandCodec.Decode(JsonUtility.FromJson<CommandData>(JsonUtility.ToJson(CommandCodec.Encode(command))));

            Assert.That(decoded.GetType(), Is.EqualTo(command.GetType()));
            Assert.That(JsonUtility.ToJson(CommandCodec.Encode(decoded)), Is.EqualTo(JsonUtility.ToJson(CommandCodec.Encode(command))));
        }

        [Test]
        public void FileStorage_WriteReplaceDelete()
        {
            var dir = Path.Combine(Path.GetTempPath(), "crucible-save-test-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var storage = new FileSaveStorage(Path.Combine(dir, "run.json"));
                Assert.That(storage.Read(), Is.Null);

                storage.Write("a");
                storage.Write("b");
                Assert.That(storage.Read(), Is.EqualTo("b"));
                Assert.That(File.Exists(Path.Combine(dir, "run.json.tmp")), Is.False);

                storage.Delete();
                Assert.That(storage.Read(), Is.Null);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
