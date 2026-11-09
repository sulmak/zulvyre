using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Zulvyre.EditorTools
{
    public static class BuildGame
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Zulvyre/Build Windows Player")]
        public static void BuildWindows()
        {
            EnsureScene();
            ConfigurePlayer();

            string buildDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build", "Zulvyre"));
            Directory.CreateDirectory(buildDirectory);
            string executable = Path.Combine(buildDirectory, "Zulvyre.exe");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = executable,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log("[Zulvyre] Build " + summary.result + " — " + summary.totalSize / (1024 * 1024) + " MB, " +
                      summary.totalErrors + " errors — " + executable);

            if (summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException("[Zulvyre] Windows build failed: " + summary.result);
            }
        }

        static void EnsureScene()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));
            if (!File.Exists(Path.Combine(Application.dataPath, "Scenes", "Main.unity")))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("[Zulvyre] Created " + ScenePath);
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Md. Atikur Rahman Mridha";
            PlayerSettings.productName = "Zulvyre";
            PlayerSettings.bundleVersion = "1.4.0";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            AssetDatabase.SaveAssets();
        }
    }
}
