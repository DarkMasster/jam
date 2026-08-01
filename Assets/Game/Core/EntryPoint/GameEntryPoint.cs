using Jam.Core.Save;
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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
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
