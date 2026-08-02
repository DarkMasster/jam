using Jam.Core.Flow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jam.Core.Flow.Editor
{
    /// <summary>
    /// Пересобирает общую сцену прибытия из кода. Сцена намеренно почти пустая:
    /// <see cref="HotelArrivalController"/> сам строит Canvas и EventSystem в Awake,
    /// поэтому здесь достаточно камеры и одного объекта с контроллером.
    /// </summary>
    public static class HotelArrivalSceneBuilder
    {
        private const string SceneFolder = "Assets/Game/Scenes";
        private const string ScenePath = SceneFolder + "/HotelArrival.unity";

        /// <summary>Фон камеры совпадает с фоном UI, чтобы не было видно шва по краям.</summary>
        private static readonly Color BackgroundColor = new(0.035f, 0.045f, 0.065f, 1f);

        /// <summary>Собирает сцену заново и сохраняет её по фиксированному пути.</summary>
        [MenuItem("Jam/Flow/Rebuild Hotel Arrival")]
        public static void Build()
        {
            EnsureFolder(SceneFolder);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            var arrival = new GameObject("HotelArrival", typeof(HotelArrivalController));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = arrival;

            Debug.Log($"Built hotel arrival scene at {ScenePath}");
        }

        /// <summary>Единственная камера сцены: она же держит AudioListener.</summary>
        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 1f, -10f);

            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            camera.orthographic = true;
        }

        /// <summary>Рекурсивно создаёт папку проекта, если её ещё нет.</summary>
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var separator = path.LastIndexOf('/');
            var parent = path[..separator];
            var folder = path[(separator + 1)..];
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
