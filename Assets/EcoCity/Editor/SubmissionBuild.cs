#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EcoCity.Editor
{
    public static class SubmissionBuild
    {
        [Serializable] private sealed class Manifest
        {
            public string product = "에코시티 구조대";
            public string version = SubmissionSceneBuilder.Version;
            public string unity = Application.unityVersion;
            public string platform = "Windows x86_64 / Mono";
            public string scene = SubmissionSceneBuilder.ScenePath;
            public string validation = "Edit Mode checks passed; interactive play, rendering and standalone execution still require manual confirmation.";
            public string saveSlot = "eco-city-alpha-v1";
            public long bytes;
            public int buildErrors;
            public int buildWarnings;
        }
        [MenuItem("EcoCity/Submission/Build Windows 64-bit (No Auto Run)")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode only.");
            EcoCity.Validation.SubmissionChecks.Run();
            string root = Path.GetFullPath("Builds/EcoCity-Submission-" + SubmissionSceneBuilder.Version);
            for (int index = 2; Directory.Exists(root); index++)
                root = Path.GetFullPath("Builds/EcoCity-Submission-" + SubmissionSceneBuilder.Version + "-" + index);
            Directory.CreateDirectory(root);
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { SubmissionTitleBuilder.ScenePath, SubmissionCharacterBuilder.ScenePath, SubmissionSceneBuilder.ScenePath }, target = BuildTarget.StandaloneWindows64,
                locationPathName = Path.Combine(root, "EcoCity.exe"), options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + report.summary.result + ", errors=" + report.summary.totalErrors);
            var manifest = new Manifest { bytes = (long)report.summary.totalSize, buildErrors = (int)report.summary.totalErrors, buildWarnings = (int)report.summary.totalWarnings };
            File.WriteAllText(Path.Combine(root, "build-info.json"), JsonUtility.ToJson(manifest, true));
            string documents = Path.GetFullPath("Submission");
            if (Directory.Exists(documents))
                foreach (var file in Directory.GetFiles(documents, "*.md")) File.Copy(file, Path.Combine(root, Path.GetFileName(file)), false);
            string license = Path.Combine(root, "Licenses"); Directory.CreateDirectory(license);
            File.Copy("Assets/EcoCity/Fonts/OFL.txt", Path.Combine(license, "NanumGothic-OFL.txt"), false);
            File.Copy("Assets/EcoCity/Fonts/SOURCE.md", Path.Combine(license, "NanumGothic-SOURCE.md"), false);
            File.Copy("Assets/EcoCity/Fonts/Jua/OFL.txt", Path.Combine(license, "Jua-OFL.txt"), false);
            File.Copy("Assets/EcoCity/Fonts/Jua/SOURCE.md", Path.Combine(license, "Jua-SOURCE.md"), false);
            Debug.Log("EcoCity Windows build PASSED: " + root + "; bytes=" + manifest.bytes + "; errors=" + manifest.buildErrors + "; warnings=" + manifest.buildWarnings + ". Application was NOT launched.");
        }
        public static void BuildBatch()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }
    }
}
#endif
