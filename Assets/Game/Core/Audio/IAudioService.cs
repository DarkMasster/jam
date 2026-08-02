using UnityEngine;

namespace Jam.Core.Audio
{
    public interface IAudioService
    {
        AudioMixContext CurrentContext { get; }
        bool IsVoicePlaying { get; }

        AudioPlaybackHandle Play(AudioCue cue, Vector3 position = default);
        bool Stop(AudioPlaybackHandle handle, float fadeSeconds = 0f);
        void PlayMusic(AudioCue cue, float fadeSeconds = 0.5f);
        void StopMusic(float fadeSeconds = 0.5f);
        void PlayVoice(AudioClip clip, float volume = 1f);
        void StopVoice();
        float GetBusVolume(AudioBus bus);
        void SetBusVolume(AudioBus bus, float volume);
        void SetContext(object owner, AudioMixContext context, float transitionSeconds = 0.15f);
        void ClearContext(object owner, float transitionSeconds = 0.15f);
    }
}
