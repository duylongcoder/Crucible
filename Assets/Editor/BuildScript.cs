using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Build từ command line / CI:
/// <c>Unity -batchmode -nographics -projectPath . -executeMethod BuildScript.WebGL -quit</c>
/// Bản *Dev là Development build: HUD hiện seed + state hash để so tất định giữa Editor / Standalone / WebGL (M5).
/// </summary>
public static class BuildScript
{
    public static void WebGL() => Build(BuildTarget.WebGL, "Builds/WebGL", BuildOptions.None);

    public static void WebGLDev() => Build(BuildTarget.WebGL, "Builds/WebGLDev", BuildOptions.Development);

    public static void StandaloneDev() =>
        Build(BuildTarget.StandaloneOSX, "Builds/StandaloneDev/Crucible.app", BuildOptions.Development);

    private static void Build(BuildTarget target, string output, BuildOptions options)
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var report = BuildPipeline.BuildPlayer(scenes, output, target, options);

        var summary = report.summary;
        Debug.Log($"{target} build: {summary.result}, {summary.totalErrors} lỗi, {summary.totalSize} bytes → {output}");

        // Batchmode cần exit code khác 0 để CI biết build hỏng.
        if (summary.result != BuildResult.Succeeded && Application.isBatchMode)
            EditorApplication.Exit(1);
    }
}
