namespace Alchemy.Tests.EditMode
{
    /// <summary>Số run của harness: mặc định cỡ CI mỗi commit; nightly truyền <c>-harnessRuns 50000</c> cho Unity.</summary>
    internal static class Harness
    {
        public static int Runs(int ciDefault)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
                if (args[i] == "-harnessRuns" && int.TryParse(args[i + 1], out var n) && n > 0)
                    return n;
            return ciDefault;
        }
    }
}
