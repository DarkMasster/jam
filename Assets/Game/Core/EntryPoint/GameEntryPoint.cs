using Jam.Core.Cutscenes;
using Jam.Core.Localization;
using Jam.Core.Save;
using Jam.Core.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jam.Core.EntryPoint
{
    [DefaultExecutionOrder(-1000)]
    public sealed class GameEntryPoint : MonoBehaviour
    {
        public static GameEntryPoint Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureRuntimeEntryPoint()
        {
            if (FindAnyObjectByType<GameEntryPoint>() == null)
            {
                new GameObject("GameEntryPoint").AddComponent<GameEntryPoint>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Loc.InitializeFromPreferences();
            if (GetComponent<LocalizationBootstrap>() == null)
            {
                gameObject.AddComponent<LocalizationBootstrap>();
            }
            if (GetComponent<GlobalHudController>() == null)
            {
                gameObject.AddComponent<GlobalHudController>();
            }
            if (GetComponent<CutsceneDirector>() == null)
            {
                gameObject.AddComponent<CutsceneDirector>();
            }
            Application.targetFrameRate = 60;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (GameSaveService.HasSave && scene.name != "Main")
            {
                GameSaveService.SetLastScene(scene.name);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                GameSaveService.Flush();
            }
        }

        private void OnApplicationQuit()
        {
            GameSaveService.Flush();
        }
    }
}
