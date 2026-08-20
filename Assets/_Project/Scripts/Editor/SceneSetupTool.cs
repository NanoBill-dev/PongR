using System.Collections.Generic;
using System.IO;
using PongRoyale.App;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PongRoyale.Editor
{
    /// <summary>
    /// Cria as cenas base do projeto e as registra no Build Settings na ordem correta.
    /// E idempotente: cena que ja existe nao e sobrescrita, apenas registrada.
    /// Menu: Pong Royale / Setup / Create Base Scenes
    /// </summary>
    public static class SceneSetupTool
    {
        private const string ScenesFolder = "Assets/_Project/Scenes";
        private const float MatchCameraOrthographicSize = 9f;

        private static readonly Color BackgroundColor = new Color(0.07f, 0.08f, 0.12f, 1f);

        [MenuItem("Pong Royale/Setup/Create Base Scenes")]
        public static void CreateBaseScenes()
        {
            Directory.CreateDirectory(ScenesFolder);

            CreateSceneIfMissing(SceneNames.Bootstrap, PopulateBootstrapScene);
            CreateSceneIfMissing(SceneNames.MainMenu, scene => AddCamera(MatchCameraOrthographicSize));
            CreateSceneIfMissing(SceneNames.Match, scene => AddCamera(MatchCameraOrthographicSize));

            RegisterScenesInBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SceneSetup] Cenas base prontas e registradas no Build Settings.");
        }

        private static void CreateSceneIfMissing(string sceneName, System.Action<Scene> populate)
        {
            string path = ScenePath(sceneName);
            if (File.Exists(path))
            {
                Debug.Log($"[SceneSetup] '{sceneName}' ja existe, preservada.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            populate(scene);
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[SceneSetup] '{sceneName}' criada em {path}.");
        }

        private static void PopulateBootstrapScene(Scene scene)
        {
            AddCamera(MatchCameraOrthographicSize);
            var bootstrapObject = new GameObject("AppBootstrap");
            bootstrapObject.AddComponent<AppBootstrap>();
        }

        private static void AddCamera(float orthographicSize)
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
        }

        private static void RegisterScenesInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenePath(SceneNames.Bootstrap), true),
                new EditorBuildSettingsScene(ScenePath(SceneNames.MainMenu), true),
                new EditorBuildSettingsScene(ScenePath(SceneNames.Match), true)
            };
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static string ScenePath(string sceneName) => $"{ScenesFolder}/{sceneName}.unity";
    }
}
