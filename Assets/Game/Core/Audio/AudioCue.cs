using UnityEngine;

namespace Jam.Core.Audio
{
    [CreateAssetMenu(fileName = "AudioCue", menuName = "Jam/Audio/Audio Cue")]
    public sealed class AudioCue : ScriptableObject
    {
        [SerializeField] private string stableId = "audio.cue";
        [SerializeField] private AudioClip[] clips;
        [SerializeField] private AudioBus bus = AudioBus.Sfx;
        [SerializeField] private Vector2 volumeRange = Vector2.one;
        [SerializeField] private Vector2 pitchRange = Vector2.one;
        [SerializeField, Range(0f, 1f)] private float spatialBlend;
        [SerializeField] private bool loop;
        [SerializeField, Min(0f)] private float cooldownSeconds;
        [SerializeField, Min(1)] private int maxSimultaneous = 4;
        [SerializeField, Range(0, 256)] private int priority = 128;

        public string StableId => stableId;
        public AudioBus Bus => bus;
        public float SpatialBlend => spatialBlend;
        public bool Loop => loop;
        public float CooldownSeconds => cooldownSeconds;
        public int MaxSimultaneous => maxSimultaneous;
        public int Priority => priority;

        public AudioClip PickClip()
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            return clips[Random.Range(0, clips.Length)];
        }

        public float PickVolume()
        {
            return Random.Range(volumeRange.x, volumeRange.y);
        }

        public float PickPitch()
        {
            return Random.Range(pitchRange.x, pitchRange.y);
        }

        private void OnValidate()
        {
            stableId = stableId?.Trim();
            volumeRange = SortAndClamp(volumeRange, 0f, 1f);
            pitchRange = SortAndClamp(pitchRange, -3f, 3f);
            maxSimultaneous = Mathf.Max(1, maxSimultaneous);
            priority = Mathf.Clamp(priority, 0, 256);
        }

        private static Vector2 SortAndClamp(Vector2 value, float min, float max)
        {
            var lower = Mathf.Clamp(Mathf.Min(value.x, value.y), min, max);
            var upper = Mathf.Clamp(Mathf.Max(value.x, value.y), min, max);
            return new Vector2(lower, upper);
        }
    }
}
