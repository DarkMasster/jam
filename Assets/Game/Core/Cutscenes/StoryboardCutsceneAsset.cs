using System;
using UnityEngine;

namespace Jam.Core.Cutscenes
{
    [CreateAssetMenu(fileName = "StoryboardCutscene", menuName = "Jam/Cutscenes/Storyboard Cutscene")]
    public sealed class StoryboardCutsceneAsset : ScriptableObject
    {
        [SerializeField] private bool skippable = true;
        [SerializeField] private StoryboardFrame[] frames = Array.Empty<StoryboardFrame>();

        public bool Skippable => skippable;
        public StoryboardFrame[] Frames => frames;
    }

    [Serializable]
    public sealed class StoryboardFrame
    {
        public Sprite background;
        public Sprite portrait;
        [Tooltip("Unity Localization String Table used by speakerKey and textKey.")]
        public string localizationTable = "Photo";
        public string speakerKey;
        public string textKey;
        public string speaker;
        [TextArea(3, 8)] public string text;
        public AudioClip voice;
        [Min(0f)] public float autoAdvanceSeconds;
    }
}
