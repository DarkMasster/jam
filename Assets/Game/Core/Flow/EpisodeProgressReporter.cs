using Jam.Core.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jam.Core.Flow
{
    public sealed class EpisodeProgressReporter : MonoBehaviour
    {
        [SerializeField] private CharacterId characterId;
        [SerializeField] private string characterSelectScene = "CharacterSelect";
        [SerializeField] private string checkpointId = "checkpoint";

        public void SaveCheckpoint()
        {
            if (characterId == CharacterId.None)
            {
                Debug.LogError($"{nameof(EpisodeProgressReporter)} on '{name}' has no character assigned.");
                return;
            }

            GameSaveService.SaveCharacterCheckpoint(
                characterId,
                gameObject.scene.name,
                checkpointId);
        }

        public void CompleteMainStoryLine()
        {
            if (characterId == CharacterId.None)
            {
                Debug.LogError($"{nameof(EpisodeProgressReporter)} on '{name}' has no character assigned.");
                return;
            }

            GameSaveService.CompleteMainStoryLine(characterId, characterSelectScene);
            SceneManager.LoadSceneAsync(characterSelectScene, LoadSceneMode.Single);
        }
    }
}
