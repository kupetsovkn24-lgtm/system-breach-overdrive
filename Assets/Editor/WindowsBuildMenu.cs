#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SystemBreachOverdrive.Editor
{
    public static class WindowsBuildMenu
    {
        private const string BuildDirectory = "Build/Windows";
        private const string ExecutableName = "SystemBreachOverdrive.exe";

        [MenuItem("System Breach/Build/Windows Release")]
        public static void BuildWindowsRelease()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Build aborted",
                    "No enabled scenes in Build Settings. Add at least one scene before building.",
                    "OK");
                return;
            }

            Directory.CreateDirectory(BuildDirectory);
            var outputPath = Path.Combine(BuildDirectory, ExecutableName);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                var message = $"Build succeeded.\n\nOutput: {Path.GetFullPath(outputPath)}\nSize: {summary.totalSize / (1024f * 1024f):0.0} MB\nTime: {summary.totalTime:mm\\:ss}";
                EditorUtility.DisplayDialog("Build complete", message, "OK");
                Debug.Log(message);
                return;
            }

            var failMessage = $"Build failed with result: {summary.result}\nOutput: {Path.GetFullPath(outputPath)}";
            EditorUtility.DisplayDialog("Build failed", failMessage, "OK");
            throw new Exception(failMessage);
        }
    }
}
#endif
