using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jam.Core.Save
{
    public static class GameModeSaveService
    {
        public static bool CanSaveActiveMode => TryFindProvider(out _);

        public static bool TrySaveActiveMode(out string message)
        {
            if (!TryFindProvider(out var provider))
            {
                message = "Этот режим пока не поддерживает ручное сохранение.";
                return false;
            }

            try
            {
                if (!provider.TrySave(out message))
                {
                    message = string.IsNullOrWhiteSpace(message)
                        ? $"{provider.ModeName}: сохранение сейчас недоступно."
                        : message;
                    return false;
                }

                GameSaveService.Flush();
                message = string.IsNullOrWhiteSpace(message)
                    ? $"{provider.ModeName}: сохранено."
                    : message;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not save active game mode: {exception}");
                message = "Не удалось сохранить игру.";
                return false;
            }
        }

        private static bool TryFindProvider(out IGameModeSaveProvider provider)
        {
            provider = null;
            var activeScene = SceneManager.GetActiveScene();
            foreach (var behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                         FindObjectsInactive.Exclude))
            {
                if (behaviour.gameObject.scene != activeScene
                    || behaviour is not IGameModeSaveProvider candidate
                    || !candidate.CanSave)
                {
                    continue;
                }

                if (provider != null)
                {
                    Debug.LogWarning(
                        $"More than one save provider is active in '{activeScene.name}'. " +
                        $"Using '{provider.ModeName}', ignoring '{candidate.ModeName}'.");
                    continue;
                }

                provider = candidate;
            }

            return provider != null;
        }
    }
}
