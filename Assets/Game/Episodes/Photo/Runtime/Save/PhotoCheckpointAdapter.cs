using System;
using Jam.Core.Save;
using UnityEngine;

namespace Jam.Episodes.Photo
{
    public static class PhotoCheckpointAdapter
    {
        public const int CurrentSchemaVersion = 3;
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
        private sealed class SchemaV2PhotoSaveData
        {
            public int schemaVersion = 2;
            public PhotoAct activeAct = PhotoAct.Prologue;
            public SchemaV2PrologueProgress prologue = new();
            public PhotoActProgress mainAct = new();
            public PhotoActProgress finale = new();
        }

        [Serializable]
        private sealed class SchemaV2PrologueProgress
        {
            public PhotoWhiteboxPhase phase = PhotoWhiteboxPhase.IntroDialogue;
            public int introIndex;
            public int inspectedMask;
            public PhotoChoice photoChoice = PhotoChoice.None;
            public int truth;
            public int reach;
            public bool publicationCommitted;
            public bool completed;
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

                if (version != null && version.schemaVersion == 2)
                {
                    var schemaV2 = JsonUtility.FromJson<SchemaV2PhotoSaveData>(checkpoint.payloadJson);
                    data = MigrateSchemaV2(schemaV2, checkpoint.checkpointId);
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
            data.prologue.introIndex = Math.Clamp(data.prologue.introIndex, 0, 3);
            data.prologue.truth = Math.Max(0, data.prologue.truth);
            data.prologue.reach = Math.Max(0, data.prologue.reach);
            PhotoPrologueRules.ClampScales(data.prologue);

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


            if (!Enum.IsDefined(typeof(PhotoPrologueStep), data.prologue.step))
            {
                data.prologue.step = PhotoPrologueStep.RoomSecret;
            }

            if (!Enum.IsDefined(typeof(PhotoSecretChoice), data.prologue.secretChoice))
            {
                data.prologue.secretChoice = PhotoSecretChoice.None;
            }

            if (!Enum.IsDefined(typeof(PhotoRoomShotChoice), data.prologue.roomShotChoice))
            {
                data.prologue.roomShotChoice = PhotoRoomShotChoice.None;
            }

            if (!Enum.IsDefined(typeof(PhotoMotherReply), data.prologue.motherReply))
            {
                data.prologue.motherReply = PhotoMotherReply.None;
            }

            if (!Enum.IsDefined(typeof(PhotoMailboxPublication), data.prologue.mailboxPublication))
            {
                data.prologue.mailboxPublication = PhotoMailboxPublication.None;
            }

            if (!Enum.IsDefined(typeof(PhotoProloguePath), data.prologue.path))
            {
                data.prologue.path = PhotoProloguePath.Undecided;
            }

            if (!Enum.IsDefined(typeof(PhotoBorderReply), data.prologue.borderReply))
            {
                data.prologue.borderReply = PhotoBorderReply.None;
            }

            data.prologue.mailboxDetailsMask &= PhotoPrologueRules.AllMailboxDetailsMask;

            if (data.prologue.mailboxPublication == PhotoMailboxPublication.None)
            {
                data.prologue.path = PhotoProloguePath.Undecided;
                data.prologue.publicationCommitted = false;
                data.prologue.borderReply = PhotoBorderReply.None;
                data.prologue.completed = false;
                if (data.prologue.step > PhotoPrologueStep.MailboxPublication)
                {
                    data.prologue.step = data.prologue.mailboxDetailsMask == 0
                        ? PhotoPrologueStep.MailboxHunt
                        : PhotoPrologueStep.MailboxPublication;
                }
            }
            else
            {
                data.prologue.path = PathForPublication(data.prologue.mailboxPublication);
                data.prologue.publicationCommitted = true;
            }

            if (data.prologue.borderReply != PhotoBorderReply.None
                && !PhotoPrologueRules.IsBorderReplyAvailable(data.prologue, data.prologue.borderReply))
            {
                data.prologue.borderReply = PhotoBorderReply.None;
                data.prologue.completed = false;
                data.prologue.step = PhotoPrologueStep.BorderControl;
            }

            if (data.prologue.completed)
            {
                if (data.prologue.borderReply == PhotoBorderReply.None)
                {
                    data.prologue.completed = false;
                    data.prologue.step = PhotoPrologueStep.BorderControl;
                }
                else
                {
                    data.prologue.step = PhotoPrologueStep.Complete;
                }
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
            ApplyLegacyNarrativeState(progress, checkpointId, progress.photoChoice);
            return Validate(data, checkpointId);
        }

        private static PhotoCharacterSaveData MigrateSchemaV2(
            SchemaV2PhotoSaveData legacy,
            string checkpointId)
        {
            var data = CreateNew();
            if (legacy == null)
            {
                return data;
            }

            data.activeAct = legacy.activeAct;
            data.mainAct = legacy.mainAct ?? new PhotoActProgress();
            data.finale = legacy.finale ?? new PhotoActProgress();

            var source = legacy.prologue ?? new SchemaV2PrologueProgress();
            var progress = data.prologue;
            progress.phase = source.phase;
            progress.introIndex = source.introIndex;
            progress.inspectedMask = source.inspectedMask;
            progress.photoChoice = source.photoChoice;
            progress.truth = source.truth;
            progress.reach = source.reach;
            progress.publicationCommitted = source.publicationCommitted;
            progress.completed = source.completed;
            ApplyLegacyNarrativeState(progress, checkpointId, source.photoChoice);
            return Validate(data, checkpointId);
        }

        private static void ApplyLegacyNarrativeState(
            PhotoPrologueProgress progress,
            string checkpointId,
            PhotoChoice choice)
        {
            progress.honesty = PhotoPrologueRules.InitialHonesty;
            progress.recognition = PhotoPrologueRules.InitialRecognition;

            if (checkpointId == "photo.arrival" || progress.completed)
            {
                ApplyLegacyPublication(progress, choice);
                progress.airportPhotoResolved = true;
                progress.borderReply = progress.path == PhotoProloguePath.Recognition
                    ? PhotoBorderReply.Recognition
                    : PhotoBorderReply.Honest;
                progress.step = PhotoPrologueStep.Complete;
                progress.completed = true;
                return;
            }

            if (checkpointId == "photo.published" || progress.publicationCommitted)
            {
                ApplyLegacyPublication(progress, choice);
                progress.step = PhotoPrologueStep.AirportPhoto;
                return;
            }

            // The production room scene did not exist in schema v1/v2, so an
            // unfinished legacy run restarts at its first stable choice.
            progress.step = PhotoPrologueStep.RoomSecret;
            progress.publicationCommitted = false;
            progress.completed = false;
        }

        private static void ApplyLegacyPublication(PhotoPrologueProgress progress, PhotoChoice choice)
        {
            progress.mailboxDetailsMask = PhotoPrologueRules.AllMailboxDetailsMask;
            if (choice == PhotoChoice.Butterfly)
            {
                progress.mailboxPublication = PhotoMailboxPublication.Wings;
                progress.path = PhotoProloguePath.Recognition;
                progress.recognition = 70;
            }
            else
            {
                progress.mailboxPublication = PhotoMailboxPublication.Honest;
                progress.path = PhotoProloguePath.Honesty;
                progress.honesty = 70;
            }

            progress.publicationCommitted = true;
        }

        private static PhotoProloguePath PathForPublication(PhotoMailboxPublication publication)
        {
            return publication switch
            {
                PhotoMailboxPublication.Wings => PhotoProloguePath.Recognition,
                PhotoMailboxPublication.Honest => PhotoProloguePath.Honesty,
                PhotoMailboxPublication.Balance => PhotoProloguePath.Balance,
                _ => PhotoProloguePath.Undecided
            };
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
