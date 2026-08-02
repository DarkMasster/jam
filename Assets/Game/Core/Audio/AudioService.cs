using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Jam.Core.Audio
{
    [DefaultExecutionOrder(-900)]
    public sealed class AudioService : MonoBehaviour, IAudioService
    {
        private const string ConfigurationResourcePath = "Audio/AudioConfiguration";
        private const string PreferencePrefix = "jam.settings.audio.";
        private const int InitialPoolSize = 12;

        private sealed class Playback
        {
            public int Id;
            public AudioCue Cue;
            public AudioSource Source;
            public float BaseVolume;
        }

        public static AudioService Instance { get; private set; }

        public AudioMixContext CurrentContext { get; private set; }
        public bool IsVoicePlaying => _voiceSource != null && _voiceSource.isPlaying;

        private readonly Dictionary<AudioBus, float> _busVolumes = new();
        private readonly Dictionary<object, AudioMixContext> _contextOwners = new();
        private readonly Dictionary<int, Playback> _activePlaybacks = new();
        private readonly Dictionary<AudioCue, float> _lastPlayTimes = new();
        private readonly Stack<AudioSource> _availableSources = new();
        private readonly List<int> _completedPlaybackIds = new();

        private AudioConfiguration _configuration;
        private AudioSource _voiceSource;
        private readonly AudioSource[] _musicSources = new AudioSource[2];
        private readonly float[] _musicBaseVolumes = new float[2];
        private readonly float[] _musicFadeGains = new float[2];
        private Coroutine _musicFade;
        private int _activeMusicIndex;
        private int _nextPlaybackId = 1;
        private float _voiceBaseVolume = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            _configuration = Resources.Load<AudioConfiguration>(ConfigurationResourcePath);
            LoadPreferences();
            CreatePersistentSources();
            CurrentContext = AudioMixContext.Default;
            RefreshSourceVolumes();
        }

        private void Update()
        {
            RecycleCompletedPlaybacks();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public AudioPlaybackHandle Play(AudioCue cue, Vector3 position = default)
        {
            if (cue == null || !CanPlay(cue))
            {
                return default;
            }

            var clip = cue.PickClip();
            if (clip == null)
            {
                return default;
            }

            var source = AcquireSource();
            source.transform.position = position;
            source.clip = clip;
            source.loop = cue.Loop;
            source.pitch = cue.PickPitch();
            source.spatialBlend = cue.SpatialBlend;
            source.priority = cue.Priority;
            source.outputAudioMixerGroup = GetGroup(cue.Bus);

            var playback = new Playback
            {
                Id = NextPlaybackId(),
                Cue = cue,
                Source = source,
                BaseVolume = cue.PickVolume()
            };
            source.volume = playback.BaseVolume * EffectiveBusVolume(cue.Bus);
            _activePlaybacks.Add(playback.Id, playback);
            _lastPlayTimes[cue] = Time.unscaledTime;
            source.Play();
            return new AudioPlaybackHandle(playback.Id);
        }

        public bool Stop(AudioPlaybackHandle handle, float fadeSeconds = 0f)
        {
            if (!handle.IsValid || !_activePlaybacks.TryGetValue(handle.Id, out var playback))
            {
                return false;
            }

            if (fadeSeconds <= 0f)
            {
                ReleasePlayback(playback);
            }
            else
            {
                StartCoroutine(FadeAndRelease(playback, fadeSeconds));
            }

            return true;
        }

        public void PlayMusic(AudioCue cue, float fadeSeconds = 0.5f)
        {
            var clip = cue?.PickClip();
            if (clip == null)
            {
                return;
            }

            var current = _musicSources[_activeMusicIndex];
            if (current.isPlaying && current.clip == clip)
            {
                return;
            }

            var nextIndex = 1 - _activeMusicIndex;
            var next = _musicSources[nextIndex];
            next.Stop();
            next.clip = clip;
            next.loop = cue.Loop;
            next.pitch = cue.PickPitch();
            next.outputAudioMixerGroup = GetGroup(AudioBus.Music);
            _musicBaseVolumes[nextIndex] = cue.PickVolume();
            _musicFadeGains[nextIndex] = fadeSeconds <= 0f ? 1f : 0f;
            next.Play();

            if (_musicFade != null)
            {
                StopCoroutine(_musicFade);
            }

            _musicFade = StartCoroutine(CrossfadeMusic(_activeMusicIndex, nextIndex, fadeSeconds));
            _activeMusicIndex = nextIndex;
        }

        public void StopMusic(float fadeSeconds = 0.5f)
        {
            if (_musicFade != null)
            {
                StopCoroutine(_musicFade);
            }

            _musicFade = StartCoroutine(FadeOutMusic(fadeSeconds));
        }

        public void PlayVoice(AudioClip clip, float volume = 1f)
        {
            _voiceSource.Stop();
            _voiceSource.clip = clip;
            _voiceBaseVolume = Mathf.Clamp01(volume);
            _voiceSource.volume = _voiceBaseVolume * EffectiveBusVolume(AudioBus.Voice);
            if (clip != null)
            {
                _voiceSource.Play();
            }
        }

        public void StopVoice()
        {
            _voiceSource.Stop();
            _voiceSource.clip = null;
        }

        public float GetBusVolume(AudioBus bus)
        {
            return _busVolumes.TryGetValue(bus, out var value) ? value : 1f;
        }

        public void SetBusVolume(AudioBus bus, float volume)
        {
            _busVolumes[bus] = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PreferenceKey(bus), _busVolumes[bus]);
            PlayerPrefs.Save();
            RefreshSourceVolumes();
        }

        public void SetContext(object owner, AudioMixContext context, float transitionSeconds = 0.15f)
        {
            if (owner == null)
            {
                return;
            }

            _contextOwners[owner] = context;
            RecalculateContext(transitionSeconds);
        }

        public void ClearContext(object owner, float transitionSeconds = 0.15f)
        {
            if (owner != null && _contextOwners.Remove(owner))
            {
                RecalculateContext(transitionSeconds);
            }
        }

        private void CreatePersistentSources()
        {
            _voiceSource = CreateSource("Voice", AudioBus.Voice);
            _voiceSource.ignoreListenerPause = true;

            for (var index = 0; index < _musicSources.Length; index++)
            {
                _musicSources[index] = CreateSource($"Music {index + 1}", AudioBus.Music);
                _musicSources[index].loop = true;
                _musicFadeGains[index] = index == 0 ? 1f : 0f;
            }

            for (var index = 0; index < InitialPoolSize; index++)
            {
                var source = CreateSource($"Pooled Audio {index + 1}", AudioBus.Sfx);
                source.gameObject.SetActive(false);
                _availableSources.Push(source);
            }
        }

        private AudioSource CreateSource(string sourceName, AudioBus bus)
        {
            var sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);
            var source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = GetGroup(bus);
            return source;
        }

        private AudioSource AcquireSource()
        {
            var source = _availableSources.Count > 0
                ? _availableSources.Pop()
                : CreateSource("Pooled Audio (Expanded)", AudioBus.Sfx);
            source.gameObject.SetActive(true);
            return source;
        }

        private void ReleasePlayback(Playback playback)
        {
            if (!_activePlaybacks.Remove(playback.Id))
            {
                return;
            }

            var source = playback.Source;
            source.Stop();
            source.clip = null;
            source.loop = false;
            source.pitch = 1f;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = null;
            source.transform.localPosition = Vector3.zero;
            source.gameObject.SetActive(false);
            _availableSources.Push(source);
        }

        private void RecycleCompletedPlaybacks()
        {
            _completedPlaybackIds.Clear();
            foreach (var pair in _activePlaybacks)
            {
                if (!pair.Value.Source.isPlaying)
                {
                    _completedPlaybackIds.Add(pair.Key);
                }
            }

            foreach (var id in _completedPlaybackIds)
            {
                if (_activePlaybacks.TryGetValue(id, out var playback))
                {
                    ReleasePlayback(playback);
                }
            }
        }

        private bool CanPlay(AudioCue cue)
        {
            if (_lastPlayTimes.TryGetValue(cue, out var lastPlay)
                && Time.unscaledTime - lastPlay < cue.CooldownSeconds)
            {
                return false;
            }

            var count = 0;
            foreach (var playback in _activePlaybacks.Values)
            {
                if (playback.Cue == cue && ++count >= cue.MaxSimultaneous)
                {
                    return false;
                }
            }

            return true;
        }

        private IEnumerator FadeAndRelease(Playback playback, float duration)
        {
            var startVolume = playback.Source.volume;
            var elapsed = 0f;
            while (_activePlaybacks.ContainsKey(playback.Id) && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                playback.Source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            ReleasePlayback(playback);
        }

        private IEnumerator CrossfadeMusic(int fromIndex, int toIndex, float duration)
        {
            if (duration <= 0f)
            {
                _musicFadeGains[fromIndex] = 0f;
                _musicFadeGains[toIndex] = 1f;
                _musicSources[fromIndex].Stop();
                RefreshMusicVolumes();
                _musicFade = null;
                yield break;
            }

            var fromStart = _musicFadeGains[fromIndex];
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                _musicFadeGains[fromIndex] = Mathf.Lerp(fromStart, 0f, t);
                _musicFadeGains[toIndex] = t;
                RefreshMusicVolumes();
                yield return null;
            }

            _musicSources[fromIndex].Stop();
            _musicFadeGains[fromIndex] = 0f;
            _musicFadeGains[toIndex] = 1f;
            RefreshMusicVolumes();
            _musicFade = null;
        }

        private IEnumerator FadeOutMusic(float duration)
        {
            var starts = new[] { _musicFadeGains[0], _musicFadeGains[1] };
            var elapsed = 0f;
            while (duration > 0f && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                for (var index = 0; index < _musicSources.Length; index++)
                {
                    _musicFadeGains[index] = Mathf.Lerp(starts[index], 0f, t);
                }
                RefreshMusicVolumes();
                yield return null;
            }

            for (var index = 0; index < _musicSources.Length; index++)
            {
                _musicSources[index].Stop();
                _musicFadeGains[index] = 0f;
            }
            RefreshMusicVolumes();
            _musicFade = null;
        }

        private void RecalculateContext(float transitionSeconds)
        {
            var next = AudioMixContext.Default;
            foreach (var context in _contextOwners.Values)
            {
                if (context > next)
                {
                    next = context;
                }
            }

            if (next == CurrentContext)
            {
                return;
            }

            CurrentContext = next;
            var snapshot = _configuration?.GetSnapshot(next);
            if (snapshot != null)
            {
                snapshot.TransitionTo(Mathf.Max(0f, transitionSeconds));
            }
            RefreshSourceVolumes();
        }

        private void RefreshSourceVolumes()
        {
            foreach (var playback in _activePlaybacks.Values)
            {
                playback.Source.volume = playback.BaseVolume * EffectiveBusVolume(playback.Cue.Bus);
            }

            if (_voiceSource != null)
            {
                _voiceSource.volume = _voiceBaseVolume * EffectiveBusVolume(AudioBus.Voice);
            }
            RefreshMusicVolumes();
        }

        private void RefreshMusicVolumes()
        {
            for (var index = 0; index < _musicSources.Length; index++)
            {
                if (_musicSources[index] != null)
                {
                    _musicSources[index].volume = _musicBaseVolumes[index]
                                                        * _musicFadeGains[index]
                                                        * EffectiveBusVolume(AudioBus.Music);
                }
            }
        }

        private float EffectiveBusVolume(AudioBus bus)
        {
            return bus == AudioBus.Master
                ? GetBusVolume(AudioBus.Master)
                : GetBusVolume(AudioBus.Master) * GetBusVolume(bus) * ContextMultiplier(bus);
        }

        private float ContextMultiplier(AudioBus bus)
        {
            if (bus == AudioBus.Master || bus == AudioBus.UI)
            {
                return 1f;
            }

            return CurrentContext switch
            {
                AudioMixContext.Paused when bus == AudioBus.Music => 0.35f,
                AudioMixContext.Paused when bus == AudioBus.Voice => 0.35f,
                AudioMixContext.Paused when bus == AudioBus.Ambience => 0.20f,
                AudioMixContext.Paused => 0.15f,
                AudioMixContext.Cutscene when bus == AudioBus.Voice => 1f,
                AudioMixContext.Cutscene when bus == AudioBus.Music => 0.55f,
                AudioMixContext.Cutscene when bus == AudioBus.Ambience => 0.65f,
                AudioMixContext.Cutscene => 0.35f,
                _ => 1f
            };
        }

        private AudioMixerGroup GetGroup(AudioBus bus)
        {
            return _configuration?.GetGroup(bus);
        }

        private void LoadPreferences()
        {
            foreach (AudioBus bus in System.Enum.GetValues(typeof(AudioBus)))
            {
                _busVolumes[bus] = Mathf.Clamp01(PlayerPrefs.GetFloat(PreferenceKey(bus), 1f));
            }
        }

        private static string PreferenceKey(AudioBus bus)
        {
            return PreferencePrefix + bus.ToString().ToLowerInvariant();
        }

        private int NextPlaybackId()
        {
            if (_nextPlaybackId == int.MaxValue)
            {
                _nextPlaybackId = 1;
            }
            return _nextPlaybackId++;
        }
    }
}
