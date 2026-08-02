using System.Linq;
using Jam.Core.Cutscenes;
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
        private const string CutsceneFolder = "Assets/Game/Episodes/Photo/Cutscenes";
        private const string IntroCutscenePath = CutsceneFolder + "/PhotoIntroStoryboard.asset";
        private const string IntroCutsceneId = "photo.prologue.intro";
        private const string OutroCutscenePath = CutsceneFolder + "/PhotoOutroStoryboard.asset";
        private const string OutroCutsceneId = "photo.prologue.to_be_continued";
        private const string ScenePath = "Assets/Game/Scenes/Prologue_Photo.unity";

        private static readonly (string speaker, string text)[] IntroFrames =
        {
            ("ОНА", "24 февраля 2022 года. Утро начинается с новостей, которым не находится места в привычной жизни."),
            ("РЕДАКТОР", "Агентство закрывает российский офис. Проекты остановлены. Команда распущена."),
            ("ТЕЛЕФОН", "Forbidgram недоступен. Клиенты молчат. В телефоне остаются архив, незакрытые счета и билет в один конец."),
            ("ОНА", "Перед отъездом нужен ещё один кадр — достаточно честный, чтобы не предать себя, и достаточно заметный, чтобы оплатить дорогу.")
        };

        private static readonly (string speaker, string text)[] OutroFrames =
        {
            ("ДУБАЙ", "Дверь транзитного номера закрывается. Впервые за день уведомления молчат."),
            ("ОТРАЖЕНИЕ / ИМПУЛЬС", "ПРОДОЛЖЕНИЕ СЛЕДУЕТ")
        };

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
            EnsureFolder(CutsceneFolder);

            var introCutscene = EnsureIntroCutsceneAsset();
            var outroCutscene = EnsureOutroCutsceneAsset();

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
                ConfigureIntroPresentation(root, introCutscene);
                ConfigureOutroPresentation(root, outroCutscene);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                Selection.activeGameObject = root;
            }
            else
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var root = GameObject.Find("PhotoWhiteboxRoot");
                blackboard = root != null ? root.GetComponent<Blackboard>() : null;
                ConfigureIntroPresentation(root, introCutscene);
                ConfigureOutroPresentation(root, outroCutscene);
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
            if (blackboard.GetVariable("cutsceneId") == null) blackboard.AddVariable("cutsceneId", string.Empty);
            if (blackboard.GetVariable("cutsceneResult") == null) blackboard.AddVariable("cutsceneResult", string.Empty);
        }

        private static StoryboardCutsceneAsset EnsureIntroCutsceneAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<StoryboardCutsceneAsset>(IntroCutscenePath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<StoryboardCutsceneAsset>();
                asset.name = "PhotoIntroStoryboard";
                AssetDatabase.CreateAsset(asset, IntroCutscenePath);
            }

            var serialized = new SerializedObject(asset);
            serialized.FindProperty("skippable").boolValue = true;
            var frames = serialized.FindProperty("frames");
            frames.arraySize = IntroFrames.Length;
            for (var index = 0; index < IntroFrames.Length; index++)
            {
                var frame = frames.GetArrayElementAtIndex(index);
                frame.FindPropertyRelative("localizationTable").stringValue = "Photo";
                frame.FindPropertyRelative("speakerKey").stringValue = $"prologue.intro.{index + 1:000}.speaker";
                frame.FindPropertyRelative("textKey").stringValue = $"prologue.intro.{index + 1:000}.text";
                frame.FindPropertyRelative("speaker").stringValue = IntroFrames[index].speaker;
                frame.FindPropertyRelative("text").stringValue = IntroFrames[index].text;
                frame.FindPropertyRelative("autoAdvanceSeconds").floatValue = 0f;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static StoryboardCutsceneAsset EnsureOutroCutsceneAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<StoryboardCutsceneAsset>(OutroCutscenePath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<StoryboardCutsceneAsset>();
                asset.name = "PhotoOutroStoryboard";
                AssetDatabase.CreateAsset(asset, OutroCutscenePath);
            }

            var serialized = new SerializedObject(asset);
            serialized.FindProperty("skippable").boolValue = true;
            var frames = serialized.FindProperty("frames");
            frames.arraySize = OutroFrames.Length;
            for (var index = 0; index < OutroFrames.Length; index++)
            {
                var frame = frames.GetArrayElementAtIndex(index);
                frame.FindPropertyRelative("localizationTable").stringValue = "Photo";
                frame.FindPropertyRelative("speakerKey").stringValue = $"production.outro.{index + 1:000}.speaker";
                frame.FindPropertyRelative("textKey").stringValue = $"production.outro.{index + 1:000}.text";
                frame.FindPropertyRelative("speaker").stringValue = OutroFrames[index].speaker;
                frame.FindPropertyRelative("text").stringValue = OutroFrames[index].text;
                frame.FindPropertyRelative("autoAdvanceSeconds").floatValue = 0f;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void ConfigureIntroPresentation(GameObject root, StoryboardCutsceneAsset sequence)
        {
            if (root == null)
            {
                Debug.LogError("PhotoWhiteboxRoot is missing; intro cutscene cannot be configured.");
                return;
            }

            var presentation = root.GetComponents<UiStoryboardPresentation>()
                .FirstOrDefault(candidate => candidate.CutsceneId == IntroCutsceneId);
            if (presentation == null)
            {
                presentation = root.AddComponent<UiStoryboardPresentation>();
            }

            var serialized = new SerializedObject(presentation);
            serialized.FindProperty("cutsceneId").stringValue = IntroCutsceneId;
            serialized.FindProperty("sequence").objectReferenceValue = sequence;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presentation);
        }

        private static void ConfigureOutroPresentation(GameObject root, StoryboardCutsceneAsset sequence)
        {
            if (root == null)
            {
                Debug.LogError("PhotoWhiteboxRoot is missing; outro cutscene cannot be configured.");
                return;
            }

            var presentation = root.GetComponents<UiStoryboardPresentation>()
                .FirstOrDefault(candidate => candidate.CutsceneId == OutroCutsceneId);
            if (presentation == null)
            {
                presentation = root.AddComponent<UiStoryboardPresentation>();
            }

            var serialized = new SerializedObject(presentation);
            serialized.FindProperty("cutsceneId").stringValue = OutroCutsceneId;
            serialized.FindProperty("sequence").objectReferenceValue = sequence;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presentation);
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
