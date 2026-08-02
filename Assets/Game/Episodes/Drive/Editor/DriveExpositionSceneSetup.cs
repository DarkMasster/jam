using System.Linq;
using Jam.Core.Cutscenes;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Jam.Episodes.Drive.Editor
{
    public static class DriveExpositionSceneSetup
    {
        private const string ScenePath = "Assets/Game/Scenes/Prologue_Drive.unity";
        private const string AssetPath = "Assets/Game/Episodes/Drive/Cutscenes/DriveExpositionStoryboard.asset";
        private const string VendorRoot = "Assets/PolygonOffice/Prefabs/";

        [MenuItem("Jam/Drive/Create Exposition Scene")]
        public static void Create()
        {
            var sequence = EnsureSequence();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("DriveExposition");
            var controller = root.AddComponent<DriveExpositionController>();
            var presenter = root.AddComponent<DriveExpositionDioramaPresenter>();

            var visualRoot = new GameObject("MoscowApartmentDiorama");
            visualRoot.transform.SetParent(root.transform, false);
            CreatePrimitive(visualRoot.transform, "Floor", PrimitiveType.Cube, new Vector3(0f, -0.1f, 1f), new Vector3(8f, 0.2f, 6f), new Color(0.16f, 0.17f, 0.20f));
            CreatePrimitive(visualRoot.transform, "BackWall", PrimitiveType.Cube, new Vector3(0f, 1.6f, 3.9f), new Vector3(8f, 3.4f, 0.2f), new Color(0.24f, 0.25f, 0.29f));
            CreatePrimitive(visualRoot.transform, "Window", PrimitiveType.Cube, new Vector3(-2.2f, 1.9f, 3.75f), new Vector3(2.4f, 1.7f, 0.08f), new Color(0.12f, 0.22f, 0.34f));
            CreatePrimitive(visualRoot.transform, "NewsScreen", PrimitiveType.Cube, new Vector3(1.9f, 1.7f, 3.7f), new Vector3(2.4f, 1.35f, 0.12f), new Color(0.30f, 0.04f, 0.05f));
            AddVendor("Props/Desk Props/SM_Prop_Computer_Setup_01.prefab", visualRoot.transform, new Vector3(0.3f, 0f, 1.9f), Vector3.zero);
            AddVendor("Props/Desk Props/SM_Prop_Cellphone_01.prefab", visualRoot.transform, new Vector3(-0.4f, 0.85f, 1.4f), Vector3.zero);
            AddVendor("Props/Desk Props/SM_Prop_Laptop_01.prefab", visualRoot.transform, new Vector3(0.8f, 0.8f, 1.4f), Vector3.zero);

            var stageCamera = CreateOutputCamera(root.transform, "StageCamera", OutputChannels.Channel07, new Color(0.035f, 0.04f, 0.055f));
            var shots = new[]
            {
                CreateShot(visualRoot.transform, "CM_MoscowMorning", new Vector3(4.8f, 2.8f, -3.8f), new Vector3(0f, 1.2f, 1.7f), 42f, OutputChannels.Channel07),
                CreateShot(visualRoot.transform, "CM_BreakingNews", new Vector3(2.1f, 1.8f, 0.5f), new Vector3(1.9f, 1.7f, 3.7f), 31f, OutputChannels.Channel07),
                CreateShot(visualRoot.transform, "CM_FamilyCall", new Vector3(-2.8f, 2.0f, -1.2f), new Vector3(-0.4f, 0.9f, 1.4f), 35f, OutputChannels.Channel07),
                CreateShot(visualRoot.transform, "CM_DepartureDecision", new Vector3(3.8f, 2.3f, -2.8f), new Vector3(0.7f, 0.9f, 1.5f), 38f, OutputChannels.Channel07)
            };

            var portraitRoot = new GameObject("PortraitRig");
            portraitRoot.transform.SetParent(root.transform, false);
            portraitRoot.transform.localPosition = new Vector3(0f, -20f, 0f);
            AddVendor("Characters/SM_Chr_Boss_Male_01.prefab", portraitRoot.transform, Vector3.zero, new Vector3(0f, 180f, 0f));
            CreatePointLight(portraitRoot.transform, new Vector3(-0.55f, 2f, -0.7f), new Color(1f, 0.68f, 0.52f), 3.4f);
            CreatePointLight(portraitRoot.transform, new Vector3(0.65f, 1.65f, -0.4f), new Color(0.32f, 0.46f, 0.78f), 1.1f);
            var portraitCamera = CreateOutputCamera(portraitRoot.transform, "PortraitCamera", OutputChannels.Channel08, new Color(0.025f, 0.03f, 0.045f));
            CreateShot(portraitRoot.transform, "CM_DrivePortrait", new Vector3(0f, 1.7f, -1.1f), new Vector3(0f, 1.7f, 0f), 31f, OutputChannels.Channel08);

            var serializedPresenter = new SerializedObject(presenter);
            serializedPresenter.FindProperty("visualRoot").objectReferenceValue = visualRoot;
            serializedPresenter.FindProperty("stageCamera").objectReferenceValue = stageCamera;
            serializedPresenter.FindProperty("portraitCamera").objectReferenceValue = portraitCamera;
            var shotsProperty = serializedPresenter.FindProperty("stageShots");
            shotsProperty.arraySize = shots.Length;
            for (var index = 0; index < shots.Length; index++) shotsProperty.GetArrayElementAtIndex(index).objectReferenceValue = shots[index];
            serializedPresenter.ApplyModifiedPropertiesWithoutUndo();

            var presentationObject = new GameObject("DriveExpositionPresentation", typeof(UiStoryboardPresentation));
            presentationObject.transform.SetParent(root.transform, false);
            var presentation = presentationObject.GetComponent<UiStoryboardPresentation>();
            presentation.Configure("drive.prologue.exposition", sequence, presenter);
            EditorUtility.SetDirty(presentation);

            var mainCamera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            mainCamera.tag = "MainCamera";
            mainCamera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            mainCamera.GetComponent<Camera>().backgroundColor = Color.black;
            var lightObject = new GameObject("Directional Light", typeof(Light));
            lightObject.transform.eulerAngles = new Vector3(45f, -35f, 0f);
            lightObject.GetComponent<Light>().type = LightType.Directional;
            lightObject.GetComponent<Light>().intensity = 1.2f;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureBuildSettings();
            Debug.Log("Prologue_Drive exposition scene created.");
        }

        private static StoryboardCutsceneAsset EnsureSequence()
        {
            var asset = AssetDatabase.LoadAssetAtPath<StoryboardCutsceneAsset>(AssetPath);
            if (asset == null) { asset = ScriptableObject.CreateInstance<StoryboardCutsceneAsset>(); AssetDatabase.CreateAsset(asset, AssetPath); }
            var serialized = new SerializedObject(asset);
            var frames = serialized.FindProperty("frames");
            var texts = new[]
            {
                "Москва просыпается обычным рабочим утром. В календаре — созвоны, дедлайны и семейные планы.",
                "Новостная лента ломает привычный порядок. Началась война, и прежняя уверенность больше ничего не гарантирует.",
                "Он обещает семье, что всё решит. Но впервые не знает, где безопасно и сколько времени осталось.",
                "В багажник отправляются документы, ноутбук и вещи детей. Маршрут один: Тбилиси через Верхний Ларс."
            };
            frames.arraySize = texts.Length;
            for (var index = 0; index < texts.Length; index++)
            {
                var frame = frames.GetArrayElementAtIndex(index);
                frame.FindPropertyRelative("localizationTable").stringValue = "Common";
                frame.FindPropertyRelative("speaker").stringValue = "ОН";
                frame.FindPropertyRelative("text").stringValue = texts[index];
                frame.FindPropertyRelative("autoAdvanceSeconds").floatValue = 0f;
            }
            serialized.FindProperty("skippable").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static Camera CreateOutputCamera(Transform parent, string name, OutputChannels channel, Color color)
        {
            var go = new GameObject(name, typeof(Camera), typeof(CinemachineBrain));
            go.transform.SetParent(parent, false);
            var camera = go.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = color; camera.nearClipPlane = 0.1f; camera.farClipPlane = 50f;
            go.GetComponent<CinemachineBrain>().ChannelMask = channel;
            return camera;
        }

        private static CinemachineCamera CreateShot(Transform parent, string name, Vector3 position, Vector3 target, float fov, OutputChannels channel)
        {
            var go = new GameObject(name, typeof(CinemachineCamera)); go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.LookAt(parent.TransformPoint(target));
            var camera = go.GetComponent<CinemachineCamera>(); camera.OutputChannel = channel; var lens = camera.Lens; lens.FieldOfView = fov; camera.Lens = lens; return camera;
        }

        private static void AddVendor(string path, Transform parent, Vector3 position, Vector3 rotation)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VendorRoot + path); if (prefab == null) { Debug.LogWarning("Missing PolygonOffice prefab: " + path); return; }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (PrefabUtility.IsPartOfPrefabInstance(instance)) PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            RemoveMissingScripts(instance); instance.transform.SetParent(parent, false); instance.transform.localPosition = position; instance.transform.localEulerAngles = rotation;
        }

        private static void RemoveMissingScripts(GameObject root)
        {
            foreach (var item in root.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(item.gameObject);
        }

        private static void CreatePrimitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            var shader = Shader.Find("Universal Render Pipeline/Lit"); var material = new Material(shader) { color = color }; go.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreatePointLight(Transform parent, Vector3 position, Color color, float intensity)
        {
            var go = new GameObject("PortraitLight", typeof(Light)); go.transform.SetParent(parent, false); go.transform.localPosition = position;
            var light = go.GetComponent<Light>(); light.type = LightType.Point; light.color = color; light.intensity = intensity; light.range = 3f; light.shadows = LightShadows.None;
        }

        private static void EnsureBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.All(item => item.path != ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
