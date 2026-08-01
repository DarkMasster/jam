using System;
using UnityEngine;

namespace Jam.Core.Save
{
    public enum CharacterId
    {
        None = 0,
        Drive = 1,
        Office = 2,
        Photo = 3
    }

    [Flags]
    public enum CompletedCharacters
    {
        None = 0,
        Drive = 1 << 0,
        Office = 1 << 1,
        Photo = 1 << 2,
        All = Drive | Office | Photo
    }

    [Serializable]
    public sealed class CharacterCheckpointData
    {
        public CharacterId characterId;
        public bool completed;
        public string sceneName;
        public string checkpointId;
        public string payloadJson;
    }

    [Serializable]
    public sealed class GameSaveData
    {
        public int schemaVersion = 2;
        public bool hasProgress;
        public string lastSceneName;
        public CharacterId activeCharacter;
        public int completedCharacters;
        public CharacterCheckpointData driveProgress = new() { characterId = CharacterId.Drive };
        public CharacterCheckpointData officeProgress = new() { characterId = CharacterId.Office };
        public CharacterCheckpointData photoProgress = new() { characterId = CharacterId.Photo };
        public string updatedAtUtc;
    }

    public static class GameSaveService
    {
        private const string SaveKey = "jam.save.v1";
        private const int CurrentSchemaVersion = 2;

        public static bool HasSave => TryLoad(out var data) && data.hasProgress;

        public static CharacterId ActiveCharacter =>
            TryLoad(out var data) ? data.activeCharacter : CharacterId.None;

        public static int CompletedCount
        {
            get
            {
                var flags = GetCompletedCharacters();
                var count = 0;
                if ((flags & CompletedCharacters.Drive) != 0) count++;
                if ((flags & CompletedCharacters.Office) != 0) count++;
                if ((flags & CompletedCharacters.Photo) != 0) count++;
                return count;
            }
        }

        public static bool FinaleUnlocked =>
            (GetCompletedCharacters() & CompletedCharacters.All) == CompletedCharacters.All;

        public static void StartNewGame(string firstSceneName)
        {
            ValidateSceneName(firstSceneName);
            Clear();

            Save(new GameSaveData
            {
                hasProgress = true,
                lastSceneName = firstSceneName,
                activeCharacter = CharacterId.None,
                completedCharacters = (int)CompletedCharacters.None
            });
        }

        public static void SelectCharacter(CharacterId characterId, string gameplaySceneName)
        {
            ValidateCharacter(characterId);
            ValidateSceneName(gameplaySceneName);

            var data = LoadOrCreate();
            data.hasProgress = true;
            data.activeCharacter = characterId;
            data.lastSceneName = gameplaySceneName;
            GetCharacterProgress(data, characterId).sceneName = gameplaySceneName;
            Save(data);
        }

        public static void SetLastScene(string sceneName)
        {
            ValidateSceneName(sceneName);

            var data = LoadOrCreate();
            data.hasProgress = true;
            data.lastSceneName = sceneName;
            if (data.activeCharacter != CharacterId.None)
            {
                GetCharacterProgress(data, data.activeCharacter).sceneName = sceneName;
            }
            Save(data);
        }

        public static void SaveCharacterCheckpoint(
            CharacterId characterId,
            string sceneName,
            string checkpointId,
            string payloadJson = null)
        {
            ValidateCharacter(characterId);
            ValidateSceneName(sceneName);

            var data = LoadOrCreate();
            var progress = GetCharacterProgress(data, characterId);
            progress.sceneName = sceneName;
            progress.checkpointId = checkpointId ?? string.Empty;
            progress.payloadJson = payloadJson ?? string.Empty;
            data.hasProgress = true;
            data.activeCharacter = characterId;
            data.lastSceneName = sceneName;
            Save(data);
        }

        public static bool TryGetCharacterCheckpoint(CharacterId characterId, out CharacterCheckpointData checkpoint)
        {
            checkpoint = null;
            if (characterId == CharacterId.None || !TryLoad(out var data))
            {
                return false;
            }

            var stored = GetCharacterProgress(data, characterId);
            if (string.IsNullOrWhiteSpace(stored.sceneName))
            {
                return false;
            }

            checkpoint = new CharacterCheckpointData
            {
                characterId = stored.characterId,
                completed = stored.completed,
                sceneName = stored.sceneName,
                checkpointId = stored.checkpointId,
                payloadJson = stored.payloadJson
            };
            return true;
        }

        public static void CompleteMainStoryLine(CharacterId characterId, string returnSceneName = "CharacterSelect")
        {
            ValidateCharacter(characterId);
            ValidateSceneName(returnSceneName);

            var data = LoadOrCreate();
            data.hasProgress = true;
            data.completedCharacters |= (int)ToCompletionFlag(characterId);
            GetCharacterProgress(data, characterId).completed = true;
            data.activeCharacter = CharacterId.None;
            data.lastSceneName = returnSceneName;
            Save(data);
        }

        public static bool IsCharacterCompleted(CharacterId characterId)
        {
            if (characterId == CharacterId.None)
            {
                return false;
            }

            if (!TryLoad(out var data))
            {
                return false;
            }

            return GetCharacterProgress(data, characterId).completed;
        }

        public static bool TryGetContinueScene(out string sceneName)
        {
            sceneName = null;

            if (!TryLoad(out var data) || !data.hasProgress || string.IsNullOrWhiteSpace(data.lastSceneName))
            {
                return false;
            }

            sceneName = data.lastSceneName;
            return true;
        }

        public static void Flush()
        {
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        private static GameSaveData LoadOrCreate()
        {
            return TryLoad(out var data) ? data : new GameSaveData();
        }

        private static CompletedCharacters GetCompletedCharacters()
        {
            return TryLoad(out var data)
                ? (CompletedCharacters)data.completedCharacters
                : CompletedCharacters.None;
        }

        private static bool TryLoad(out GameSaveData data)
        {
            data = null;

            if (!PlayerPrefs.HasKey(SaveKey))
            {
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<GameSaveData>(PlayerPrefs.GetString(SaveKey));
                if (data == null || data.schemaVersion > CurrentSchemaVersion)
                {
                    return false;
                }

                if (data.schemaVersion < CurrentSchemaVersion)
                {
                    data.schemaVersion = CurrentSchemaVersion;
                }

                EnsureCharacterProgress(data);
                ApplyLegacyCompletionFlags(data);

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not read save data. The save will be ignored. {exception.Message}");
                return false;
            }
        }

        private static void Save(GameSaveData data)
        {
            EnsureCharacterProgress(data);
            SynchronizeCompletionFlags(data);
            data.schemaVersion = CurrentSchemaVersion;
            data.updatedAtUtc = DateTime.UtcNow.ToString("O");
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private static CompletedCharacters ToCompletionFlag(CharacterId characterId)
        {
            return characterId switch
            {
                CharacterId.Drive => CompletedCharacters.Drive,
                CharacterId.Office => CompletedCharacters.Office,
                CharacterId.Photo => CompletedCharacters.Photo,
                _ => CompletedCharacters.None
            };
        }

        private static CharacterCheckpointData GetCharacterProgress(GameSaveData data, CharacterId characterId)
        {
            EnsureCharacterProgress(data);
            return characterId switch
            {
                CharacterId.Drive => data.driveProgress,
                CharacterId.Office => data.officeProgress,
                CharacterId.Photo => data.photoProgress,
                _ => throw new ArgumentOutOfRangeException(nameof(characterId), characterId, "Unknown character.")
            };
        }

        private static void EnsureCharacterProgress(GameSaveData data)
        {
            data.driveProgress ??= new CharacterCheckpointData();
            data.officeProgress ??= new CharacterCheckpointData();
            data.photoProgress ??= new CharacterCheckpointData();
            data.driveProgress.characterId = CharacterId.Drive;
            data.officeProgress.characterId = CharacterId.Office;
            data.photoProgress.characterId = CharacterId.Photo;
        }

        private static void ApplyLegacyCompletionFlags(GameSaveData data)
        {
            var flags = (CompletedCharacters)data.completedCharacters;
            data.driveProgress.completed |= (flags & CompletedCharacters.Drive) != 0;
            data.officeProgress.completed |= (flags & CompletedCharacters.Office) != 0;
            data.photoProgress.completed |= (flags & CompletedCharacters.Photo) != 0;
        }

        private static void SynchronizeCompletionFlags(GameSaveData data)
        {
            var flags = CompletedCharacters.None;
            if (data.driveProgress.completed) flags |= CompletedCharacters.Drive;
            if (data.officeProgress.completed) flags |= CompletedCharacters.Office;
            if (data.photoProgress.completed) flags |= CompletedCharacters.Photo;
            data.completedCharacters = (int)flags;
        }

        private static void ValidateCharacter(CharacterId characterId)
        {
            if (characterId == CharacterId.None || !Enum.IsDefined(typeof(CharacterId), characterId))
            {
                throw new ArgumentOutOfRangeException(nameof(characterId), characterId, "Unknown character.");
            }
        }

        private static void ValidateSceneName(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException("Scene name cannot be empty.", nameof(sceneName));
            }
        }
    }
}
