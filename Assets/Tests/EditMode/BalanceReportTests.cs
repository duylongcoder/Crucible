using System.IO;
using System.Linq;
using System.Text;
using Alchemy.Core;
using NUnit.Framework;

namespace Alchemy.Tests.EditMode
{
    /// <summary>
    /// M6: GreedyAgent chơi content MVP ở mọi bậc khó (bộ khởi đầu mặc định, Danh vọng 0), InvariantChecker sau mỗi
    /// Command. Ghi báo cáo cân bằng ra results/balance-report.md.
    /// </summary>
    public class BalanceReportTests
    {
        private const int MaxCommands = 5000;

        [Test]
        public void GreedyAgent_AllDifficulties_ReachRunEnd_WritesReport()
        {
            var def = TestContent.MvpRun();
            var runs = Harness.Runs(300);
            var report = new StringBuilder();
            report.AppendLine($"# Báo cáo cân bằng — GreedyAgent, {runs} run mỗi bậc khó, content `{def.Id}`").AppendLine();
            report.AppendLine("| Bậc khó | Thắng run | Trận thắng TB | Tier cao nhất TB | Ra tier 10 | Danh vọng TB |");
            report.AppendLine("| --- | --- | --- | --- | --- | --- |");
            var exiles = new StringBuilder();
            var winRates = new double[def.Difficulties.Count];

            for (var d = 0; d < def.Difficulties.Count; d++)
            {
                var difficulty = def.Difficulties[d];
                int won = 0, wins = 0, tiers = 0, supreme = 0, renown = 0;
                var exiledAt = new int[def.SeasonsPerRun + 1];
                for (ulong seed = 0; seed < (ulong)runs; seed++)
                {
                    var m = new RunStateMachine(def, seed, new RunSetup { Difficulty = difficulty }, checkInvariants: true);
                    for (var i = 0; i < MaxCommands && m.State.Phase != RunPhase.RunEnd; i++)
                        m.Execute(GreedyAgent.Next(m.State));

                    var s = m.State;
                    Assert.That(s.Phase, Is.EqualTo(RunPhase.RunEnd), $"{difficulty.Id} seed {seed} kẹt ở {s.Phase}");
                    if (s.Outcome == RunOutcome.Won)
                        won++;
                    else
                        exiledAt[s.Season]++;
                    wins += s.Wins;
                    tiers += s.HighestTier;
                    supreme += s.HighestTier >= 10 ? 1 : 0;
                    renown += MetaProgress.RenownFor(s);
                }

                winRates[d] = (double)won / runs;
                report.AppendLine($"| {difficulty.Name} | {Pct(won, runs)} | {(double)wins / runs:0.00} | {(double)tiers / runs:0.0} | " +
                                  $"{Pct(supreme, runs)} | {(double)renown / runs:0} |");
                exiles.Append($"- {difficulty.Name}: ");
                exiles.AppendLine(string.Join(", ", Enumerable.Range(1, def.SeasonsPerRun)
                    .Where(season => exiledAt[season] > 0).Select(season => $"Mùa {season}: {exiledAt[season]}")));
            }

            report.AppendLine().AppendLine("Mùa bị trục xuất (số run):").AppendLine(exiles.ToString());
            Directory.CreateDirectory("results");
            File.WriteAllText("results/balance-report.md", report.ToString());
            TestContext.WriteLine(report.ToString());

            for (var d = 1; d < winRates.Length; d++)
                Assert.That(winRates[d], Is.LessThanOrEqualTo(winRates[d - 1]), "Bậc khó sau không được dễ hơn bậc trước");
        }

        private static string Pct(int n, int total) => $"{100.0 * n / total:0.0}%";
    }
}
