using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Jam.Core.UI.Editor
{
    public static class SharedDarkUiSetup
    {
        private const string ScenePath = "Assets/Game/Scenes/CharacterSelect.unity";
        private const string VendorRoot = "Assets/PolygonOffice/Prefabs/Characters/";
        private const string ThemeFolder = "Assets/Game/Core/UI/Resources/UI";
        private const string ThemePath = ThemeFolder + "/DarkUiTheme.asset";

        [MenuItem("Jam/UI/Apply Shared DarkUI Screens")]
        public static void Apply()
        {
            EnsureTheme();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var controller = Object.FindFirstObjectByType<CharacterSelectController>();
            if (controller == null)
            {
                Debug.LogError($"CharacterSelectController was not found in {ScenePath}.");
                return;
            }

            var oldRig = GameObject.Find("CharacterSelectPortraitRig");
            if (oldRig != null) Object.DestroyImmediate(oldRig);

            var rig = new GameObject("CharacterSelectPortraitRig");
            var renderer = rig.AddComponent<CharacterSelectPortraitRenderer>();
            var cameras = new Camera[3];
            var prefabPaths = new[]
            {
                "SM_Chr_Boss_Male_01.prefab",
                "SM_Chr_Developer_Male_01.prefab",
                "SM_Chr_Developer_Female_01.prefab"
            };
            var channels = new[] { OutputChannels.Channel04, OutputChannels.Channel05, OutputChannels.Channel06 };

            for (var index = 0; index < 3; index++)
            {
                var root = new GameObject($"Portrait_{index}");
                root.transform.SetParent(rig.transform, false);
                root.transform.localPosition = new Vector3(index * 10f, -20f, 0f);
                AddVendor(prefabPaths[index], root.transform);

                var key = CreateLight(root.transform, "Key", new Color(1f, 0.66f, 0.50f), 3.4f, new Vector3(-0.55f, 2f, -0.7f));
                key.range = 3f;
                var fill = CreateLight(root.transform, "Fill", new Color(0.32f, 0.46f, 0.78f), 1.1f, new Vector3(0.65f, 1.65f, -0.4f));
                fill.range = 2.5f;

                var cameraObject = new GameObject("OutputCamera", typeof(Camera), typeof(CinemachineBrain));
                cameraObject.transform.SetParent(root.transform, false);
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.03f, 0.045f, 1f);
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 6f;
                cameraObject.GetComponent<CinemachineBrain>().ChannelMask = channels[index];
                cameras[index] = camera;

                var virtualCameraObject = new GameObject("PortraitCamera", typeof(CinemachineCamera));
                virtualCameraObject.transform.SetParent(root.transform, false);
                virtualCameraObject.transform.localPosition = new Vector3(0f, 1.7f, -1.05f);
                virtualCameraObject.transform.LookAt(root.transform.TransformPoint(new Vector3(0f, 1.7f, 0f)));
                var virtualCamera = virtualCameraObject.GetComponent<CinemachineCamera>();
                virtualCamera.OutputChannel = channels[index];
                var lens = virtualCamera.Lens;
                lens.FieldOfView = index == 0 ? 31f : 28f;
                virtualCamera.Lens = lens;
            }

            var rendererSerialized = new SerializedObject(renderer);
            var cameraProperty = rendererSerialized.FindProperty("portraitCameras");
            cameraProperty.arraySize = cameras.Length;
            for (var index = 0; index < cameras.Length; index++) cameraProperty.GetArrayElementAtIndex(index).objectReferenceValue = cameras[index];
            rendererSerialized.ApplyModifiedPropertiesWithoutUndo();

            var controllerSerialized = new SerializedObject(controller);
            controllerSerialized.FindProperty("portraitRenderer").objectReferenceValue = renderer;
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("CharacterSelect portraits and shared DarkUI theme configured.");
        }

        private static void EnsureTheme()
        {
            EnsureFolder("Assets/Game/Core/UI", "Resources");
            EnsureFolder("Assets/Game/Core/UI/Resources", "UI");
            var theme = AssetDatabase.LoadAssetAtPath<DarkUiTheme>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<DarkUiTheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            var serialized = new SerializedObject(theme);
            serialized.FindProperty("button").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Dark UI/Free/BTN_A1.png");
            serialized.FindProperty("divider").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Dark UI/Free/Divider.png");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureFolder(string parent, string name)
        {
            var path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }

        private static void AddVendor(string fileName, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VendorRoot + fileName);
            if (prefab == null) throw new System.InvalidOperationException($"PolygonOffice portrait prefab missing: {fileName}");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localEulerAngles = new Vector3(0f, 180f, 0f);
        }

        private static Light CreateLight(Transform parent, string name, Color color, float intensity, Vector3 position)
        {
            var go = new GameObject(name, typeof(Light));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var light = go.GetComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            return light;
        }
    }
}
