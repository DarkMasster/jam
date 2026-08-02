using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Jam.Core.UI.Editor
{
    public static class MainMenuDarkUiSetup
    {
        private const string ScenePath = "Assets/Game/Scenes/Main.unity";

        [MenuItem("Jam/UI/Apply DarkUI Main Menu")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var controller = Object.FindFirstObjectByType<MainMenuController>();
            if (controller == null)
            {
                Debug.LogError($"MainMenuController was not found in {ScenePath}.");
                return;
            }

            var serialized = new SerializedObject(controller);
            Assign(serialized, "darkButtonSprite", "Assets/Dark UI/Free/BTN_A1.png");
            Assign(serialized, "darkDividerSprite", "Assets/Dark UI/Free/Divider.png");
            Assign(serialized, "playIcon", "Assets/Dark UI/New Icons/White Play.png");
            Assign(serialized, "continueIcon", "Assets/Dark UI/New Icons/White Forward.png");
            Assign(serialized, "languageIcon", "Assets/Dark UI/New Icons/White Globe.png");
            Assign(serialized, "quitIcon", "Assets/Dark UI/New Icons/White Power Button.png");
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("DarkUI assets assigned to the main menu.");
        }

        private static void Assign(SerializedObject target, string propertyName, string assetPath)
        {
            var property = target.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"Serialized property '{propertyName}' was not found.");
                return;
            }

            property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (property.objectReferenceValue == null)
            {
                Debug.LogError($"DarkUI sprite was not found: {assetPath}");
            }
        }
    }
}
