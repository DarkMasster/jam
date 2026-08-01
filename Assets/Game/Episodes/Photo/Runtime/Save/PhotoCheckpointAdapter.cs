using System;
using Jam.Core.Save;
using UnityEngine;

namespace Jam.Episodes.Photo
{
    public static class PhotoCheckpointAdapter
    {
        public const int CurrentSchemaVersion = 2;
        public const int RequiredInspectionMask = 0b111;

        [Serializable]
        private sealed class LegacyPhotoWhiteboxSaveData
        {
            public int version = 1;
            public string phase;
            public int inspectedMask;
            public string choice;
            public int truth;
            public int reach;
        }

        [Serializable]
        private sealed class VersionProbe
        {
            public int schemaVersion;
            public int version;
        }

        public static PhotoCharacterSaveData CreateNew()
        {
            return new PhotoCharacterSaveData();
        }

        public static bool TryLoad(CharacterCheckpointData checkpoint, out PhotoCharacterSaveData data)
        {
            data = null;
            if (checkpoint == null || string.IsNullOrWhiteSpace(checkpoint.payloadJson))
            {
                return false;
            }

            try
            {
                var version = JsonUtility.FromJson<VersionProbe>(checkpoint.payloadJson);
                if (version != null && version.schemaVersion == CurrentSchemaVersion)
                {
                    var current = JsonUtility.FromJson<PhotoCharacterSaveData>(checkpoint.payloadJson);
                    data = Validate(current, checkpoint.checkpointId);
                    return true;
                }

                if (version != null && version.version == 1)
                {
                    var legacy = JsonUtility.FromJson<LegacyPhotoWhiteboxSaveData>(checkpoint.payloadJson);
                    data = MigrateLegacy(legacy, checkpoint.checkpointId);
                    return true;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Photo checkpoint was ignored: {exception.Message}");
            }

            return false;
        }

        public static string Serialize(PhotoCharacterSaveData data, string checkpointId)
        {
            return JsonUtility.ToJson(Validate(data, checkpointId));
        }

        public static PhotoWhiteboxPhase ResolveResumePhase(PhotoCharacterSaveData data, string checkpointId)
        {
            var validated = Validate(data, checkpointId);
            if (validated.prologue.completed)
            {
                return PhotoWhiteboxPhase.Arrival;
            }

            if (validated.prologue.publicationCommitted)
            {
                return PhotoWhiteboxPhase.ReflectionDialogue;
            }

            return validated.prologue.phase;
        }

        public static PhotoCharacterSaveData Validate(PhotoCharacterSaveData data, string checkpointId)
        {
            data ??= CreateNew();
            data.schemaVersion = CurrentSchemaVersion;
            data.prologue ??= new PhotoPrologueProgress();
            data.mainAct ??= new PhotoActProgress();
            data.finale ??= new PhotoActProgress();
            data.prologue.inspectedMask &= RequiredInspectionMask;
            data.prologue.truth = Math.Max(0, data.prologue.truth);
            data.prologue.reach = Math.Max(0, data.prologue.reach);

            if (!Enum.IsDefined(typeof(PhotoAct), data.activeAct))
            {
                data.activeAct = PhotoAct.Prologue;
            }

            if (!Enum.IsDefined(typeof(PhotoChoice), data.prologue.photoChoice))
            {
                data.prologue.photoChoice = PhotoChoice.None;
            }

            if (!Enum.IsDefined(typeof(PhotoWhiteboxPhase), data.prologue.phase))
            {
                data.prologue.phase = PhotoWhiteboxPhase.IntroDialogue;
            }

            if (data.prologue.phase >= PhotoWhiteboxPhase.Camera
                && data.prologue.inspectedMask != RequiredInspectionMask)
            {
                data.prologue.phase = PhotoWhiteboxPhase.Explore;
                data.prologue.photoChoice = PhotoChoice.None;
                data.prologue.publicationCommitted = false;
                data.prologue.completed = false;
            }

            if (data.prologue.phase >= PhotoWhiteboxPhase.Publish
                && data.prologue.photoChoice == PhotoChoice.None)
            {
                data.prologue.phase = PhotoWhiteboxPhase.Camera;
                data.prologue.publicationCommitted = false;
                data.prologue.completed = false;
            }

            if (data.prologue.publicationCommitted && data.prologue.photoChoice == PhotoChoice.None)
            {
                data.prologue.publicationCommitted = false;
            }

            if (data.prologue.completed
                && data.prologue.inspectedMask == RequiredInspectionMask
                && data.prologue.photoChoice != PhotoChoice.None)
            {
                data.prologue.phase = PhotoWhiteboxPhase.Arrival;
                data.prologue.publicationCommitted = true;
            }
            else if (data.prologue.completed)
            {
                data.prologue.completed = false;
            }

            return data;
        }

        private static PhotoCharacterSaveData MigrateLegacy(
            LegacyPhotoWhiteboxSaveData legacy,
            string checkpointId)
        {
            var data = CreateNew();
            var progress = data.prologue;
            progress.inspectedMask = legacy.inspectedMask;
            progress.photoChoice = Enum.TryParse(legacy.choice, out PhotoChoice choice)
                ? choice
                : PhotoChoice.None;
            progress.phase = Enum.TryParse(legacy.phase, out PhotoWhiteboxPhase phase)
                ? phase
                : PhaseFromCheckpoint(checkpointId);
            progress.truth = legacy.truth;
            progress.reach = legacy.reach;
            progress.publicationCommitted = checkpointId == "photo.published"
                                            || checkpointId == "photo.arrival";
            progress.completed = checkpointId == "photo.arrival";
            return Validate(data, checkpointId);
        }

        private static PhotoWhiteboxPhase PhaseFromCheckpoint(string checkpointId)
        {
            return checkpointId switch
            {
                "photo.camera" => PhotoWhiteboxPhase.Camera,
                "photo.published" => PhotoWhiteboxPhase.ReflectionDialogue,
                "photo.arrival" => PhotoWhiteboxPhase.Arrival,
                _ => PhotoWhiteboxPhase.Explore
            };
        }
    }
}
