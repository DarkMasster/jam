using System;
using Jam.Core.Save;
using UnityEngine;

namespace Jam.Episodes.Office
{
    /// <summary>
    /// Единственное место, где офисный прогресс превращается в JSON payload и обратно.
    /// Адаптер также чинит несогласованные состояния, чтобы загрузка всегда попадала
    /// в устойчивую фазу.
    /// </summary>
    public static class OfficeCheckpointAdapter
    {
        public const int CurrentSchemaVersion = 1;

        public const string SetupCheckpoint = "office.setup";
        public const string RunCheckpoint = "office.run";
        public const string ArrivalCheckpoint = "office.arrival";

        [Serializable]
        private sealed class VersionProbe
        {
            public int schemaVersion;
        }

        public static OfficeCharacterSaveData CreateNew()
        {
            return new OfficeCharacterSaveData();
        }

        public static bool TryLoad(CharacterCheckpointData checkpoint, out OfficeCharacterSaveData data)
        {
            data = null;
            if (checkpoint == null || string.IsNullOrWhiteSpace(checkpoint.payloadJson))
            {
                return false;
            }

            try
            {
                var probe = JsonUtility.FromJson<VersionProbe>(checkpoint.payloadJson);
                if (probe == null || probe.schemaVersion != CurrentSchemaVersion)
                {
                    return false;
                }

                data = Validate(JsonUtility.FromJson<OfficeCharacterSaveData>(checkpoint.payloadJson));
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Office checkpoint was ignored: {exception.Message}");
                return false;
            }
        }

        public static string Serialize(OfficeCharacterSaveData data, string checkpointId)
        {
            data ??= CreateNew();
            data.prologue ??= new OfficePrologueProgress();

            // Сначала фаза из checkpoint, только потом проверка инвариантов: иначе
            // payload мог бы одновременно содержать «забег идёт» и «сон завершён».
            data.prologue.phase = PhaseFromCheckpoint(checkpointId, data.prologue.phase);
            return JsonUtility.ToJson(Validate(data));
        }

        /// <summary>
        /// Фаза, с которой безопасно продолжить забег. Пробуждение не является
        /// устойчивой границей и всегда возвращает игрока в начало забега.
        /// </summary>
        public static OfficeEpisodePhase ResolveResumePhase(OfficeCharacterSaveData data)
        {
            var validated = Validate(data);

            // Пройденный сон начинается заново целиком: повтор без Setup выглядел бы
            // как обрыв, а не как новый заход.
            if (validated.prologue.completed || validated.prologue.phase == OfficeEpisodePhase.Setup)
            {
                return OfficeEpisodePhase.Setup;
            }

            return OfficeEpisodePhase.Run;
        }

        public static OfficeCharacterSaveData Validate(OfficeCharacterSaveData data)
        {
            data ??= CreateNew();
            data.schemaVersion = CurrentSchemaVersion;
            data.prologue ??= new OfficePrologueProgress();

            var progress = data.prologue;
            progress.retries = Math.Max(0, progress.retries);
            progress.destroyedEquipment = Math.Max(0, progress.destroyedEquipment);
            progress.wreckedChasers = Math.Max(0, progress.wreckedChasers);

            if (!Enum.IsDefined(typeof(OfficeEpisodePhase), progress.phase))
            {
                progress.phase = OfficeEpisodePhase.Setup;
            }

            // Пробуждение — постановочный переход, а не граница сохранения.
            if (progress.phase == OfficeEpisodePhase.Awakening)
            {
                progress.phase = progress.completed ? OfficeEpisodePhase.Arrival : OfficeEpisodePhase.Run;
            }

            if (progress.phase != OfficeEpisodePhase.Setup)
            {
                progress.setupSeen = true;
            }

            // Финал возможен только с обеими личными вещами: иначе кольцо не собиралось.
            if (progress.phase == OfficeEpisodePhase.Arrival && (!progress.hasLaptop || !progress.hasMug))
            {
                progress.phase = OfficeEpisodePhase.Run;
                progress.completed = false;
            }

            if (progress.phase == OfficeEpisodePhase.Arrival)
            {
                progress.completed = true;
            }
            else if (progress.completed)
            {
                progress.completed = false;
            }

            return data;
        }

        public static string CheckpointFromPhase(OfficeEpisodePhase phase)
        {
            return phase switch
            {
                OfficeEpisodePhase.Setup => SetupCheckpoint,
                OfficeEpisodePhase.Arrival => ArrivalCheckpoint,
                _ => RunCheckpoint
            };
        }

        private static OfficeEpisodePhase PhaseFromCheckpoint(string checkpointId, OfficeEpisodePhase fallback)
        {
            return checkpointId switch
            {
                SetupCheckpoint => OfficeEpisodePhase.Setup,
                RunCheckpoint => OfficeEpisodePhase.Run,
                ArrivalCheckpoint => OfficeEpisodePhase.Arrival,
                _ => fallback
            };
        }
    }
}
