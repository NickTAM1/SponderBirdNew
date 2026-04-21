using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System;

public class CIBuilder 
{
    [MenuItem("CI/BuildWebGL")]
    public static void PerformWebGLBuild()
    {
        try
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("No scenes are added to the build.");
                return;
            }

            string buildPath = "Builds/WebGL";
            if (!Directory.Exists(buildPath))
            {
                Directory.CreateDirectory(buildPath);
                Debug.Log($"Created directory at {buildPath}");
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = scenes;
            buildPlayerOptions.locationPathName = buildPath;
            buildPlayerOptions.target = BuildTarget.WebGL;
            buildPlayerOptions.options = BuildOptions.None;

            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            UnityEditor.Build.Reporting.BuildSummary summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"Build Succeeded. {summary.totalSize} bytes written");
            }
            else if (summary.result == UnityEditor.Build.Reporting.BuildResult.Failed)
            {
                Debug.Log($"Build Failed with {summary.totalErrors} errors.");
            }
        }
        catch (Exception ex)
        { 
            Debug.Log($"Critical error {ex.Message}");
        }
    }
}
