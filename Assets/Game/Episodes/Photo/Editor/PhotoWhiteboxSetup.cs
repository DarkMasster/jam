using System.Linq;
using Jam.Episodes.Photo;
using Jam.Integrations.NodeCanvas;
using NodeCanvas.Framework;
using NodeCanvas.StateMachines;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jam.Episodes.Photo.Editor
{
    public static class PhotoWhiteboxSetup
    {
        private const string GraphPath = "Assets/Game/Episodes/Photo/Graphs/PhotoPrologueWhitebox.asset";
        private const string ScenePath = "Assets/Game/Scenes/Prologue_Photo.unity";

        private static readonly string[] PhaseNames =
        {
            "Restore",
            "IntroDialogue",
            "Explore",
            "Camera",
            "Publish",
            "ReflectionDialogue",
            "Arrival"
        };

        [MenuItem("Jam/Photo/Create White-box Assets")]
        public static void CreateWhiteboxAssets()
        {
            EnsureFolder("Assets/Game/Episodes");
            EnsureFolder("Assets/Game/Episodes/Photo");
            EnsureFolder("Assets/Game/Episodes/Photo/Graphs");

            var graph = AssetDatabase.LoadAssetAtPath<FSM>(GraphPath);
            if (graph == null)
            {
                graph = ScriptableObject.CreateInstance<FSM>();
                graph.name = "PhotoPrologueWhitebox";

                for (var index = 0; index < PhaseNames.Length; index++)
                {
                    var state = graph.AddNode<PassivePhaseState>(new Vector2(index * 260f, 0f));
                    state.name = PhaseNames[index];
                }

                graph.SelfSerialize();
                AssetDatabase.CreateAsset(graph, GraphPath);
                EditorUtility.SetDirty(graph);
                AssetDatabase.SaveAssets();
            }

            Scene scene;
            Blackboard blackboard;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                CreateCamera();

                var root = new GameObject("PhotoWhiteboxRoot");
                blackboard = root.AddComponent<Blackboard>();
                var owner = root.AddComponent<FSMOwner>();
                owner.behaviour = graph;
                owner.blackboard = blackboard;
                owner.firstActivation = GraphOwner.FirstActivation.OnEnable;
                owner.enableAction = GraphOwner.EnableAction.EnableBehaviour;
                root.AddComponent<PhotoWhiteboxController>();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                Selection.activeGameObject = root;
            }
            else
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var root = GameObject.Find("PhotoWhiteboxRoot");
                blackboard = root != null ? root.GetComponent<Blackboard>() : null;
            }

            EnsureBlackboardVariables(blackboard);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureSceneInBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("Photo white-box is ready: Main -> CharacterSelect -> Photo -> Prologue_Photo.");
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.085f, 0.11f, 1f);
            camera.orthographic = true;
        }

        private static void EnsureSceneInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.All(entry => entry.path != ScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        private static void EnsureBlackboardVariables(Blackboard blackboard)
        {
            if (blackboard == null)
            {
                Debug.LogError("PhotoWhiteboxRoot is missing its Blackboard component.");
                return;
            }

            if (blackboard.GetVariable("phase") == null) blackboard.AddVariable("phase", "Restore");
            if (blackboard.GetVariable("choiceId") == null) blackboard.AddVariable("choiceId", "None");
            if (blackboard.GetVariable("truth") == null) blackboard.AddVariable("truth", 0);
            if (blackboard.GetVariable("reach") == null) blackboard.AddVariable("reach", 0);
            if (blackboard.GetVariable("inspectedCount") == null) blackboard.AddVariable("inspectedCount", 0);
            if (blackboard.GetVariable("canUseCamera") == null) blackboard.AddVariable("canUseCamera", false);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var separator = path.LastIndexOf('/');
            var parent = path[..separator];
            var name = path[(separator + 1)..];
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
