using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Build từ command line / CI:
/// <c>Unity -batchmode -nographics -projectPath . -executeMethod BuildScript.WebGL -quit</c>
/// </summary>
public static class BuildScript
{
    private const string WebGLOutput = "Builds/WebGL";

    public static void WebGL()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var report = BuildPipeline.BuildPlayer(scenes, WebGLOutput, BuildTarget.WebGL, BuildOptions.None);

        var summary = report.summary;
        Debug.Log($"WebGL build: {summary.result}, {summary.totalErrors} lỗi, {summary.totalSize} bytes → {WebGLOutput}");

        // Batchmode cần exit code khác 0 để CI biết build hỏng.
        if (summary.result != BuildResult.Succeeded && Application.isBatchMode)
            EditorApplication.Exit(1);
    }
}
