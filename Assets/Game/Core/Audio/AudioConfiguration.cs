using UnityEngine;
using UnityEngine.Audio;

namespace Jam.Core.Audio
{
    [CreateAssetMenu(fileName = "AudioConfiguration", menuName = "Jam/Audio/Configuration")]
    public sealed class AudioConfiguration : ScriptableObject
    {
        [Header("Optional AudioMixer routing")]
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup music;
        [SerializeField] private AudioMixerGroup sfx;
        [SerializeField] private AudioMixerGroup ui;
        [SerializeField] private AudioMixerGroup ambience;
        [SerializeField] private AudioMixerGroup voice;

        [Header("Optional snapshots")]
        [SerializeField] private AudioMixerSnapshot defaultSnapshot;
        [SerializeField] private AudioMixerSnapshot cutsceneSnapshot;
        [SerializeField] private AudioMixerSnapshot pausedSnapshot;

        public AudioMixer Mixer => mixer;

        public AudioMixerGroup GetGroup(AudioBus bus)
        {
            return bus switch
            {
                AudioBus.Music => music,
                AudioBus.Sfx => sfx,
                AudioBus.UI => ui,
                AudioBus.Ambience => ambience,
                AudioBus.Voice => voice,
                _ => null
            };
        }

        public AudioMixerSnapshot GetSnapshot(AudioMixContext context)
        {
            return context switch
            {
                AudioMixContext.Paused => pausedSnapshot,
                AudioMixContext.Cutscene => cutsceneSnapshot,
                _ => defaultSnapshot
            };
        }
    }
}
