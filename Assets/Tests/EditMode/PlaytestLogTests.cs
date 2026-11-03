using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Alchemy.Core;
using NUnit.Framework;

namespace Alchemy.Tests.EditMode
{
    /// <summary>
    /// Playtest MVP theo 2 vai, ghi một file log (Docs/playtest). Explicit: chạy tay khi cần, không chạy mỗi commit.
    /// User: UserAgent (không biết công thức), 10 run liên tiếp, meta giữ qua các run.
    /// Tester: phân tích tĩnh content + GreedyAgent (biết hết).
    /// </summary>
    [Explicit("Sinh log playtest; chạy tay.")]
    public class PlaytestLogTests
    {
        private const int Runs = 10;
        private const int MaxCommands = 6000;
        private const string Output = "Docs/playtest/mvp-playtest.md";

        [Test]
        public void Playtest_UserAndTester_WritesLog()
        {
            var def = TestContent.MvpRun();
            var log = new StringBuilder();
            log.AppendLine("# Playtest MVP — vai User và Tester").AppendLine();
            log.AppendLine($"Content `{def.Id}`, {def.Items.Count} vật phẩm, {def.Difficulties.Count} bậc khó, {def.StarterKits.Count} bộ khởi đầu. " +
                           "Sinh bởi `PlaytestLogTests` (chạy lại cho cùng kết quả: mọi random theo seed).").AppendLine();
            log.AppendLine("<!-- FLOW -->").AppendLine();

            var userRenown = User(def, log);
            Tester(def, log, userRenown);

            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            File.WriteAllText(Output, log.ToString());
            TestContext.WriteLine(log.ToString());
        }

        // ---------------- User ----------------

        private static double User(RunDef def, StringBuilder log)
        {
            log.AppendLine("## Vai User — 10 run liên tiếp").AppendLine();
            log.AppendLine("Người chơi mới: không biết công thức hay khoảng cách, tự thử ghép rồi nhớ cái thành công qua các run. " +
                           "Chỉ thấy tên, tier, mức sao; dâng thứ tier thấp nhất khi đối thủ ≤ ★2, ngược lại dâng tier cao nhất. " +
                           "Mỗi run chọn bậc khó cao nhất và bộ khởi đầu mới nhất đã mở. `·` = ô trống.").AppendLine();

            var meta = new MetaState();
            var agent = new UserAgent(7);
            var summary = new StringBuilder("| Run | Bậc khó | Bộ khởi đầu | Kết quả | Trận thắng | Lượt nung (hỏng) | Danh vọng | Công thức đã biết |\n| --- | --- | --- | --- | --- | --- | --- | --- |\n");
            var details = new StringBuilder();
            var totalRenown = 0;

            for (var run = 1; run <= Runs; run++)
            {
                var difficulty = def.Difficulties[System.Math.Min(meta.UnlockedDifficulties, def.Difficulties.Count) - 1];
                var kit = def.StarterKits.Last(k => k.UnlockRenown <= meta.Renown);
                var m = new RunStateMachine(def, (ulong)(1000 + run), new RunSetup { Difficulty = difficulty, StarterKit = kit, Renown = meta.Renown },
                    checkInvariants: true);
                var s = m.State;
                details.AppendLine($"### Run {run} — {difficulty.Name}, {kit.Name}, seed {s.Seed}").AppendLine();

                int brews = 0, scraps = 0;
                var season = new StringBuilder();
                for (var i = 0; i < MaxCommands && s.Phase != RunPhase.RunEnd; i++)
                {
                    var phase = s.Phase;
                    var command = agent.Next(s);
                    var accepted = m.Execute(command).Accepted;
                    Assert.That(accepted || !(command is BrewCommand), "UserAgent nung bị từ chối");
                    if (command is BrewCommand && accepted)
                    {
                        var result = s.LastBrew.Product?.Def ?? def.Scrap;
                        agent.Remember(agent.LastBrewLayout, result);
                        brews++;
                        scraps += result.Kind == ItemKind.Scrap ? 1 : 0;
                        season.Append(season.Length == 0 ? "" : "; ").Append(Layout(def, agent.LastBrewLayout)).Append(" → ").Append(result.Name);
                    }

                    if (command is SkipBrewCommand && accepted)
                        season.Append(season.Length == 0 ? "" : "; ").Append("bỏ qua lượt");

                    // Kết quả Mùa: ghi ngay khi trận xong (phase Battle).
                    if (phase != RunPhase.Battle && s.Phase == RunPhase.Battle)
                    {
                        var b = s.LastBattle;
                        var fight = b.Offered == null ? "không có gì để dâng" : $"dâng {b.Offered.Name} → {b.Equipment.Name}";
                        details.AppendLine($"- **Mùa {s.Season}** · {s.Opponent.Name} {Stars(s)}: {fight} → **{(b.Won ? "Thắng" : "Thua")}**" +
                                           $" (Uy tín {s.Reputation}). Nung: {(season.Length == 0 ? "—" : season.ToString())}");
                        season.Clear();
                    }
                }

                Assert.That(s.Phase, Is.EqualTo(RunPhase.RunEnd), $"User run {run} kẹt ở {s.Phase}");
                var rewards = MetaProgress.Apply(meta, s);
                totalRenown += rewards.RenownGained;
                var outcome = s.Outcome == RunOutcome.Won ? "Thắng run" : $"Trục xuất Mùa {s.Season}";
                var unlocks = new List<string>(rewards.Unlocks);
                if (rewards.DifficultyUnlocked != null)
                    unlocks.Insert(0, "bậc " + rewards.DifficultyUnlocked.Name);
                details.AppendLine().AppendLine($"**Chung cuộc:** {outcome}, thắng {s.Wins}/{def.SeasonsPerRun} trận, tier cao nhất {s.HighestTier}, " +
                                                $"+{rewards.RenownGained} Danh vọng (tổng {meta.Renown}){(unlocks.Count > 0 ? ", mở: " + string.Join(", ", unlocks) : "")}.").AppendLine();
                summary.AppendLine($"| {run} | {difficulty.Name} | {kit.Name} | {outcome} | {s.Wins} | {brews} ({scraps}) | +{rewards.RenownGained} → {meta.Renown} | {agent.KnownRecipes} |");
            }

            log.AppendLine(summary.ToString()).AppendLine(details.ToString());
            return (double)totalRenown / Runs;
        }

        // ---------------- Tester ----------------

        private static void Tester(RunDef def, StringBuilder log, double userRenown)
        {
            log.AppendLine("## Vai Tester — phân tích khả năng với data hiện tại").AppendLine();
            log.AppendLine("<!-- FINDINGS -->").AppendLine();

            Recipes(def, log);
            Opponents(def, log);
            Catalysts(def, log);
            var greedyRenown = Greedy(def, log);
            Progression(def, log, userRenown, greedyRenown);
        }

        private static void Recipes(RunDef def, StringBuilder log)
        {
            log.AppendLine("### Cây công thức và kinh tế").AppendLine();
            log.AppendLine($"Giá ở hệ số Mùa 100%, giá bán = {def.SellPercent}% giá thị trường. Chi phí = tổng giá mua nguyên liệu gốc (đệ quy). " +
                           "Quality cơ bản = BaseEssence (Potency 1, chưa có Xúc tác). Power ước lượng = Quality + kỳ vọng thuộc tính × trọng số, khoảng theo loại trang bị. `*` = slot cần Kích hoạt ma thuật.").AppendLine();
            log.AppendLine("| Thành phẩm | Tier | Pattern | Quality | Power ước lượng | Chi phí | Giá bán | Lãi | Ma thuật tối đa khi nung |");
            log.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
            foreach (var item in def.Items.Where(i => i.IsBrewed).OrderBy(i => i.Tier).ThenBy(i => i.Recipe.BaseEssence))
            {
                var r = item.Recipe;
                var pattern = string.Join(" ", r.Pattern.Select(slot => slot.IsGap ? "·" : def.FindItem(slot.ItemId).Name + (slot.RequiresMagic ? "*" : "")));
                var cost = Cost(def, item);
                var sell = item.MarketPrice * def.SellPercent / 100;
                var powers = def.Equipment.Select(e => r.BaseEssence * def.QualityWeight + r.Stats.Sum(st =>
                    st.ChancePercent / 100.0 * st.Value * e.StatWeights.Where(w => w.StatId == st.StatId).Sum(w => w.Weight))).ToList();
                var gaps = r.Pattern.Count(slot => slot.IsGap);
                var bestAdditive = r.AllowedAdditives.Select(id => def.FindItem(id).MagicChance).DefaultIfEmpty(0).Max();
                var magic = System.Math.Min(def.Brew.MagicCapPercent, gaps * bestAdditive);
                log.AppendLine($"| {item.Name} | {item.Tier} | {pattern} | {r.BaseEssence:0} | {powers.Min():0}–{powers.Max():0} | {cost} | {sell} | {sell - cost:+0;-0;0} | {(magic == 0 ? "0% (không có phụ gia hợp lệ)" : magic + "% (phụ gia, chưa tính truyền thừa)")} |");
            }

            var kitValue = def.StarterKits.Select(k => $"{k.Name} {k.Items.Sum(i => i.MarketPrice * def.SellPercent / 100)}");
            log.AppendLine().AppendLine($"Giá bán toàn bộ bộ khởi đầu (vốn tối đa lúc đầu, vàng khởi đầu {def.StartGold}): {string.Join(" · ", kitValue)} vàng.").AppendLine();
        }

        private static int Cost(RunDef def, ItemDef item) =>
            item.Recipe == null ? item.MarketPrice : item.Recipe.Pattern.Where(s => !s.IsGap).Sum(s => Cost(def, def.FindItem(s.ItemId)));

        private static void Opponents(RunDef def, StringBuilder log)
        {
            log.AppendLine("### Đối thủ từng Mùa so với thành phẩm").AppendLine();
            log.AppendLine("„Rẻ nhất đủ thắng chắc” = thành phẩm chi phí thấp nhất có Quality cơ bản ≥ Power (thắng với mọi loại trang bị, không cần thuộc tính).").AppendLine();
            foreach (var d in def.Difficulties)
            {
                log.AppendLine($"**{d.Name}** (Danh vọng ×{d.RenownPercent}%)").AppendLine();
                log.AppendLine("| Mùa | Đối thủ | Loại | Power | Sao | Rẻ nhất đủ thắng chắc |");
                log.AppendLine("| --- | --- | --- | --- | --- | --- |");
                for (var i = 0; i < d.Opponents.Count; i++)
                {
                    var o = d.Opponents[i];
                    var stars = 1 + def.StarThresholds.Count(t => o.Power >= t);
                    var cheapest = def.Items.Where(it => it.IsBrewed && it.Recipe.BaseEssence * def.QualityWeight >= o.Power)
                        .OrderBy(it => Cost(def, it)).FirstOrDefault();
                    log.AppendLine($"| {i + 1} | {o.Name} | {Kind(o.Kind)} | {o.Power:0} | {new string('★', stars)} | " +
                                   $"{(cheapest == null ? "không có" : $"{cheapest.Name} (tier {cheapest.Tier}, Q {cheapest.Recipe.BaseEssence:0}, chi phí {Cost(def, cheapest)})")} |");
                }

                log.AppendLine();
            }
        }

        private static void Catalysts(RunDef def, StringBuilder log)
        {
            log.AppendLine("### Xúc tác").AppendLine();
            log.AppendLine("| Xúc tác | Tier | Giá | Mở bằng | Hiệu ứng |");
            log.AppendLine("| --- | --- | --- | --- | --- |");
            foreach (var c in def.Items.Where(i => i.Kind == ItemKind.Catalyst))
            {
                var inShop = def.ShopPool.Contains(c);
                var reward = def.Rewards.FirstOrDefault(r => r.Item == c);
                var source = inShop ? (c.UnlockRenown > 0 ? $"Chợ, ≥ {c.UnlockRenown} Danh vọng" : "Chợ") : reward != null
                    ? $"chiến lợi phẩm Mùa {reward.MinSeason}+{(reward.UnlockRenown > 0 ? $", ≥ {reward.UnlockRenown} Danh vọng" : "")}" : "bộ khởi đầu";
                log.AppendLine($"| {c.Name} | {c.Tier} | {c.MarketPrice} | {source} | {c.Description} |");
            }

            log.AppendLine();
        }

        private static double Greedy(RunDef def, StringBuilder log)
        {
            log.AppendLine("### GreedyAgent (biết mọi công thức, nhìn Quality và Power)").AppendLine();
            log.AppendLine("12 run chi tiết: mỗi bậc khó × mỗi bộ khởi đầu (+3 run thêm ở Thường), mọi thứ đã mở khóa.").AppendLine();
            log.AppendLine("| # | Bậc khó | Bộ khởi đầu | Seed | Kết quả | Trận thắng | Tier cao nhất | Thắng/thua từng Mùa |");
            log.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");
            var n = 0;
            var setups = def.Difficulties.SelectMany(d => def.StarterKits.Select(k => (d, k))).ToList();
            setups.AddRange(def.StarterKits.Select(k => (def.Difficulties[0], k)));
            foreach (var (d, k) in setups)
            {
                n++;
                var seed = (ulong)(500 + n);
                var m = new RunStateMachine(def, seed, new RunSetup { Difficulty = d, StarterKit = k, Renown = 10000 }, checkInvariants: true);
                var seasons = new StringBuilder();
                for (var i = 0; i < MaxCommands && m.State.Phase != RunPhase.RunEnd; i++)
                {
                    var before = m.State.Phase;
                    m.Execute(GreedyAgent.Next(m.State));
                    if (before != RunPhase.Battle && m.State.Phase == RunPhase.Battle)
                        seasons.Append(m.State.LastBattle.Won ? "T" : "B");
                }

                var s = m.State;
                Assert.That(s.Phase, Is.EqualTo(RunPhase.RunEnd));
                log.AppendLine($"| {n} | {d.Name} | {k.Name} | {seed} | {(s.Outcome == RunOutcome.Won ? "Thắng run" : $"Trục xuất Mùa {s.Season}")} | {s.Wins} | {s.HighestTier} | `{seasons}` |");
            }

            log.AppendLine().AppendLine("`T` = thắng, `B` = thua trận của Mùa đó.").AppendLine();
            log.AppendLine("Tổng hợp 300 run mỗi ô (bộ khởi đầu × bậc khó, đã mở hết khóa):").AppendLine();
            log.AppendLine("| Bộ khởi đầu | " + string.Join(" | ", def.Difficulties.Select(d => d.Name)) + " |");
            log.AppendLine("| --- |" + string.Concat(def.Difficulties.Select(_ => " --- |")));
            var renownSum = 0.0;
            foreach (var k in def.StarterKits)
            {
                var row = new List<string>();
                foreach (var d in def.Difficulties)
                {
                    int won = 0, tier = 0, renown = 0;
                    const int runs = 300;
                    for (ulong seed = 0; seed < runs; seed++)
                    {
                        var m = new RunStateMachine(def, seed, new RunSetup { Difficulty = d, StarterKit = k, Renown = 10000 });
                        for (var i = 0; i < MaxCommands && m.State.Phase != RunPhase.RunEnd; i++)
                            m.Execute(GreedyAgent.Next(m.State));
                        won += m.State.Outcome == RunOutcome.Won ? 1 : 0;
                        tier += m.State.HighestTier;
                        renown += MetaProgress.RenownFor(m.State);
                    }

                    if (d == def.Difficulties[0] && k == def.StarterKits[0])
                        renownSum = (double)renown / runs;
                    row.Add($"thắng {100.0 * won / runs:0}% · tier TB {(double)tier / runs:0.0} · DV {renown / runs}");
                }

                log.AppendLine($"| {k.Name} | {string.Join(" | ", row)} |");
            }

            log.AppendLine();
            return renownSum;
        }

        private static void Progression(RunDef def, StringBuilder log, double userRenown, double greedyRenown)
        {
            log.AppendLine("### Tiến trình mở khóa").AppendLine();
            log.AppendLine($"Danh vọng TB mỗi run: User {userRenown:0} · GreedyAgent (Thường, bộ đầu) {greedyRenown:0}.").AppendLine();
            log.AppendLine("| Mốc | Danh vọng | Số run cần (User) | Số run cần (Greedy) |");
            log.AppendLine("| --- | --- | --- | --- |");
            var marks = def.StarterKits.Where(k => k.UnlockRenown > 0).Select(k => (k.Name, k.UnlockRenown))
                .Concat(def.ShopPool.Where(i => i.UnlockRenown > 0).Select(i => (i.Name + " vào Chợ", i.UnlockRenown)))
                .Concat(def.Rewards.Where(r => r.UnlockRenown > 0).Select(r => (r.Name, r.UnlockRenown)))
                .OrderBy(x => x.UnlockRenown);
            foreach (var (name, renown) in marks)
                log.AppendLine($"| {name} | {renown} | {RunsNeeded(renown, userRenown)} | {RunsNeeded(renown, greedyRenown)} |");
            log.AppendLine();
        }

        private static string RunsNeeded(int renown, double perRun) => perRun <= 0 ? "∞" : System.Math.Ceiling(renown / perRun).ToString("0");

        private static string Layout(RunDef def, string[] layout) =>
            string.Join(" ", layout.Select(id => id == UserAgent.GapId ? "·" : def.FindItem(id).Name));

        private static string Kind(BattleKind k) => k == BattleKind.Tournament ? "Thi đấu" : k == BattleKind.War ? "Chiến tranh" : "Đại chiến";

        private static string Stars(RunState s) => new string('★', Forge.Stars(s));
    }
}
