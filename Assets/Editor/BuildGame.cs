using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// CS4483 → Build Windows (x64)
/// Builds a standalone Windows 64-bit player to  CS4483/Build/ThePit/ThePit.exe
/// Includes MainMenu and MainScene; all debug hotkeys are stripped automatically by the UNITY_EDITOR guard in those scripts.
/// </summary>
public static class BuildGame
{
    private const string BuildFolder = "Build/ThePit";
    private const string ExeName     = "ThePit.exe";

    [MenuItem("CS4483/🏗 Build Windows x64")]
    public static void BuildWindows64()
    {
        // Resolve output path relative to project root
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string outputDir   = Path.Combine(projectRoot, BuildFolder);
        string outputExe   = Path.Combine(outputDir, ExeName);

        Directory.CreateDirectory(outputDir);

        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes = new[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/MainScene.unity",
            },
            locationPathName = outputExe,
            target           = BuildTarget.StandaloneWindows64,
            options          = BuildOptions.None,
        };

        Debug.Log($"[BuildGame] Starting Windows 64-bit build → {outputExe}");

        BuildReport report = BuildPipeline.BuildPlayer(opts);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildGame] ✓ Build succeeded ({report.summary.totalSize / 1024 / 1024} MB) → {outputDir}");
            EditorUtility.RevealInFinder(outputDir);
        }
        else
        {
            Debug.LogError($"[BuildGame] ✗ Build FAILED — {report.summary.totalErrors} error(s). Check the Console for details.");
        }
    }
}
