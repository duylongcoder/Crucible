using Alchemy.Content;
using UnityEditor;
using UnityEngine;

/// <summary>Chặn Enter Play Mode khi content sai (CLAUDE.md mục 7). EditMode test chạy cùng validator.</summary>
[InitializeOnLoad]
public static class ContentValidationOnPlay
{
    static ContentValidationOnPlay()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.ExitingEditMode)
            return;

        var failed = false;
        foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(RunDefAsset)))
        {
            var run = AssetDatabase.LoadAssetAtPath<RunDefAsset>(AssetDatabase.GUIDToAssetPath(guid));
            foreach (var error in ContentValidator.Validate(run))
            {
                Debug.LogError("Content: " + error, run);
                failed = true;
            }
        }

        if (failed)
            EditorApplication.isPlaying = false;
    }
}
