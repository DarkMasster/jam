using System.Collections;
using Jam.Core.Cutscenes;
using Jam.Core.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jam.Episodes.Drive
{
    public sealed class DriveExpositionController : MonoBehaviour
    {
        private const string CutsceneId = "drive.prologue.exposition";

        private IEnumerator Start()
        {
            yield return null;
            var director = CutsceneDirector.Instance;
            if (director == null) { Debug.LogError("CutsceneDirector is unavailable for Drive exposition."); yield break; }
            director.Finished += HandleFinished;
            var context = new CutsceneContext { characterId = CharacterId.Drive.ToString(), startCheckpointId = "drive.intro", completionCheckpointId = "drive.departure" };
            if (!director.TryPlay(CutsceneId, context, out var error)) Debug.LogError(error);
        }

        private void OnDestroy()
        {
            if (CutsceneDirector.Instance != null) CutsceneDirector.Instance.Finished -= HandleFinished;
        }

        private void HandleFinished(CutsceneResult result)
        {
            if (result.CutsceneId != CutsceneId) return;
            CutsceneDirector.Instance.Finished -= HandleFinished;
            if (result.Succeeded) GameSaveService.SaveCharacterCheckpoint(CharacterId.Drive, gameObject.scene.name, "drive.departure");
            SceneManager.LoadSceneAsync("CharacterSelect", LoadSceneMode.Single);
        }
    }
}
