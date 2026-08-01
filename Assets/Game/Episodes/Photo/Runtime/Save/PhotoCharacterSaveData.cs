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
        public int schemaVersion = 2;
        public PhotoAct activeAct = PhotoAct.Prologue;
        public PhotoPrologueProgress prologue = new();
        public PhotoActProgress mainAct = new();
        public PhotoActProgress finale = new();
    }

    [Serializable]
    public sealed class PhotoPrologueProgress
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
    public sealed class PhotoActProgress
    {
        public bool started;
        public bool completed;
        public string checkpointId = string.Empty;
    }
}
