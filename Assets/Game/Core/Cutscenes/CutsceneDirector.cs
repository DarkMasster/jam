using System;
using Jam.Core.Audio;
using Jam.Core.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Jam.Core.Cutscenes
{
    public sealed class CutsceneDirector : MonoBehaviour
    {
        public static CutsceneDirector Instance { get; private set; }

        public event Action<CutsceneResult> Finished;

        public bool IsPlaying => _currentPresentation != null;
        public string CurrentCutsceneId => _currentPresentation?.CutsceneId;

        private ICutscenePresentation _currentPresentation;
        private MonoBehaviour _currentBehaviour;
        private CutsceneContext _currentContext;
        private GlobalHudController _hud;

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
            _hud = GetComponent<GlobalHudController>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (Instance == this)
            {
                Complete(CutsceneEndReason.SceneChanged);
                Instance = null;
            }
        }

        private void Update()
        {
            if (IsPlaying
                && _currentPresentation.CanSkip
                && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                SkipCurrent();
            }
        }

        public bool TryPlay(string cutsceneId, CutsceneContext context, out string error)
        {
            error = null;
            if (IsPlaying)
            {
                error = $"Cutscene '{CurrentCutsceneId}' is already playing.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(cutsceneId))
            {
                error = "Cutscene ID cannot be empty.";
                return false;
            }

            if (!TryFindPresentation(cutsceneId, out var presentation, out var behaviour))
            {
                error = $"No cutscene presentation '{cutsceneId}' in active scene '{SceneManager.GetActiveScene().name}'.";
                return false;
            }

            _currentPresentation = presentation;
            _currentBehaviour = behaviour;
            _currentContext = context ?? new CutsceneContext();
            _hud ??= GetComponent<GlobalHudController>();
            _hud?.SetCutsceneActive(true);
            AudioService.Instance?.SetContext(this, AudioMixContext.Cutscene);

            try
            {
                presentation.Play(_currentContext, Complete);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not start cutscene '{cutsceneId}': {exception}");
                error = exception.Message;
                Complete(CutsceneEndReason.Failed);
                return false;
            }
        }

        public bool SkipCurrent()
        {
            if (!IsPlaying || !_currentPresentation.CanSkip)
            {
                return false;
            }

            _currentPresentation.Skip();
            return true;
        }

        public bool CancelCurrent(string cutsceneId)
        {
            if (!IsPlaying || CurrentCutsceneId != cutsceneId)
            {
                return false;
            }

            _currentPresentation.Stop(CutsceneEndReason.SceneChanged);
            return true;
        }

        private void Complete(CutsceneEndReason reason)
        {
            if (_currentPresentation == null)
            {
                return;
            }

            var result = new CutsceneResult(CurrentCutsceneId, reason, _currentContext);
            _currentPresentation = null;
            _currentBehaviour = null;
            _currentContext = null;
            _hud?.SetCutsceneActive(false);
            AudioService.Instance?.ClearContext(this);
            Finished?.Invoke(result);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsPlaying && (_currentBehaviour == null || _currentBehaviour.gameObject.scene != scene))
            {
                Complete(CutsceneEndReason.SceneChanged);
            }
        }

        private static bool TryFindPresentation(
            string cutsceneId,
            out ICutscenePresentation presentation,
            out MonoBehaviour behaviour)
        {
            presentation = null;
            behaviour = null;
            var activeScene = SceneManager.GetActiveScene();
            foreach (var candidateBehaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                         FindObjectsInactive.Include))
            {
                if (candidateBehaviour.gameObject.scene != activeScene
                    || candidateBehaviour is not ICutscenePresentation candidate
                    || candidate.CutsceneId != cutsceneId)
                {
                    continue;
                }

                if (presentation != null)
                {
                    Debug.LogWarning(
                        $"Duplicate cutscene ID '{cutsceneId}' in scene '{activeScene.name}'. " +
                        $"Using '{behaviour.name}', ignoring '{candidateBehaviour.name}'.");
                    continue;
                }

                presentation = candidate;
                behaviour = candidateBehaviour;
            }

            return presentation != null;
        }
    }
}
