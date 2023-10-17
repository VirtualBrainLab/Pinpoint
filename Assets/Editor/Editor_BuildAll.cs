using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;

public class Editor_BuildAll
{
    [MenuItem("Tools/Build All")]
    public static void BuildAll()
    {
        BuildWindows();
        BuildLinux();
        BuildWebGL();
    }

    private static void BuildWindows()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/TrajectoryPlanner.unity" },
            locationPathName = "Builds/Win64/Pinpoint.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        SummarizeReport(report);
    }

    private static void BuildLinux()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/TrajectoryPlanner.unity" },
            locationPathName = "Builds/Linux/Pinpoint",
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.None
        };

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64);
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        SummarizeReport(report);
    }

    private static void BuildWebGL()
    {

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/TrajectoryPlanner.unity" },
            locationPathName = "Builds/WebGL/Pinpoint",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        if (EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            SummarizeReport(report);
        }
        else
            Debug.LogError("Failed to switch contexts?");
    }

    private static void SummarizeReport(BuildReport report)
    {
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
        }

        if (summary.result == BuildResult.Failed)
        {
            Debug.Log("Build failed");
        }
    }
}
