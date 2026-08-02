using System.IO;
using Jam.Core.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jam.Core.Flow
{
    /// <summary>
    /// Общий переход между эпизодом, гостиничной сценой прибытия и выбором героя.
    /// Эпизод сообщает один <see cref="EpisodeResult"/> и никогда не грузит чужую
    /// сцену сам: решение о целевой сцене принимает этот сервис.
    /// </summary>
    public static class GameFlowService
    {
        public const string CharacterSelectScene = "CharacterSelect";
        public const string ArrivalScene = "HotelArrival";

        /// <summary>
        /// Результат последнего завершённого эпизода. Переживает загрузку сцены,
        /// потому что живёт в статике, а не в объекте сцены.
        /// </summary>
        public static EpisodeResult PendingResult { get; private set; }

        /// <summary>
        /// Сохраняет checkpoint эпизода и открывает сцену прибытия. Если
        /// `HotelArrival` ещё не добавлена в Build Settings, flow безопасно
        /// возвращает игрока в выбор героя, не ломая прохождение.
        /// </summary>
        public static void CompleteEpisode(EpisodeResult result)
        {
            if (result == null || !result.IsValid)
            {
                Debug.LogError("GameFlowService received an invalid episode result.");
                return;
            }

            PendingResult = result;

            GameSaveService.SaveCharacterCheckpoint(
                result.characterId,
                result.sceneName,
                result.checkpointId,
                result.payloadJson);
            GameSaveService.Flush();

            if (IsSceneInBuild(ArrivalScene))
            {
                SceneManager.LoadSceneAsync(ArrivalScene, LoadSceneMode.Single);
                return;
            }

            Debug.LogWarning(
                $"Scene '{ArrivalScene}' is not in Build Settings. The flow returns to '{CharacterSelectScene}'.");
            FinishArrival(result);
        }

        /// <summary>
        /// Завершает линию эпизода после сцены прибытия и возвращает игрока в выбор
        /// героя. Финал всей игры остаётся отдельным решением и здесь не выдаётся.
        /// </summary>
        public static void FinishArrival(EpisodeResult result)
        {
            var characterId = result?.characterId ?? GameSaveService.ActiveCharacter;
            PendingResult = null;

            if (characterId != CharacterId.None)
            {
                // Загрузка сцены прибытия успела записать её как последнюю сцену линии.
                // Возвращаем checkpoint на сцену самого эпизода, иначе повторный выбор
                // героя открыл бы экран прибытия вместо эпизода.
                if (result != null && !string.IsNullOrWhiteSpace(result.sceneName))
                {
                    GameSaveService.SaveCharacterCheckpoint(
                        characterId,
                        result.sceneName,
                        result.checkpointId,
                        result.payloadJson);
                }

                GameSaveService.LeaveCharacterLine(characterId, CharacterSelectScene);
                GameSaveService.Flush();
            }

            if (!IsSceneInBuild(CharacterSelectScene))
            {
                Debug.LogError($"Scene '{CharacterSelectScene}' is not in Build Settings.");
                return;
            }

            SceneManager.LoadSceneAsync(CharacterSelectScene, LoadSceneMode.Single);
        }

        /// <summary>Забирает результат один раз: повторная загрузка его не показывает.</summary>
        public static bool TryConsumePendingResult(out EpisodeResult result)
        {
            result = PendingResult;
            return result != null;
        }

        public static void ClearPendingResult()
        {
            PendingResult = null;
        }

        public static bool IsSceneInBuild(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            for (var index = 0; index < SceneManager.sceneCountInBuildSettings; index++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(index);
                if (Path.GetFileNameWithoutExtension(path) == sceneName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
