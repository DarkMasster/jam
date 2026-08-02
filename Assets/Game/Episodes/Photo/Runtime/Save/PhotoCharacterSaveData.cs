using System;

namespace Jam.Episodes.Photo
{
    public enum PhotoAct
    {
        Prologue = 1,
        Main = 2,
        Finale = 3
    }

    [Serializable]
    public sealed class PhotoCharacterSaveData
    {
        public int schemaVersion = 3;
        public PhotoAct activeAct = PhotoAct.Prologue;
        public PhotoPrologueProgress prologue = new();
        public PhotoActProgress mainAct = new();
        public PhotoActProgress finale = new();
    }

    [Serializable]
    public sealed class PhotoPrologueProgress
    {
        // Legacy white-box fields remain serialized for schema v1/v2 migration and
        // are removed only after shipped saves no longer need them.
        public PhotoWhiteboxPhase phase = PhotoWhiteboxPhase.IntroDialogue;
        public int introIndex;
        public int inspectedMask;
        public PhotoChoice photoChoice = PhotoChoice.None;
        public int truth;
        public int reach;

        // Production prologue state (schema v3).
        public PhotoPrologueStep step = PhotoPrologueStep.RoomSecret;
        public int honesty = PhotoPrologueRules.InitialHonesty;
        public int recognition = PhotoPrologueRules.InitialRecognition;
        public PhotoSecretChoice secretChoice = PhotoSecretChoice.None;
        public PhotoRoomShotChoice roomShotChoice = PhotoRoomShotChoice.None;
        public PhotoMotherReply motherReply = PhotoMotherReply.None;
        public int mailboxDetailsMask;
        public PhotoMailboxPublication mailboxPublication = PhotoMailboxPublication.None;
        public PhotoProloguePath path = PhotoProloguePath.Undecided;
        public bool airportPhotoResolved;
        public bool airportPhotoTaken;
        public PhotoBorderReply borderReply = PhotoBorderReply.None;
        public bool publicationCommitted;
        public bool completed;
    }

    [Serializable]
    public sealed class PhotoActProgress
    {
        public bool started;
        public bool completed;
        public string checkpointId = string.Empty;
    }
}
