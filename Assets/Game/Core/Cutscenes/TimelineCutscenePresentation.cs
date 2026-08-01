using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Jam.Core.Cutscenes
{
    public sealed class TimelineCutscenePresentation : MonoBehaviour, ICutscenePresentation
    {
        [SerializeField] private string cutsceneId = "cutscene.timeline";
        [SerializeField] private PlayableDirector playableDirector;
        [SerializeField] private bool skippable = true;
        [SerializeField] private bool evaluateEndOnSkip = true;

        public string CutsceneId => cutsceneId;
        public bool IsPlaying => _onFinished != null;
        public bool CanSkip => skippable;

        private Action<CutsceneEndReason> _onFinished;
        private CutsceneEndReason _stopReason = CutsceneEndReason.Completed;

        private void Awake()
        {
            playableDirector ??= GetComponent<PlayableDirector>();
        }

        private void OnDisable()
        {
            if (IsPlaying)
            {
                Stop(CutsceneEndReason.SceneChanged);
            }
        }

        public void Play(CutsceneContext context, Action<CutsceneEndReason> onFinished)
        {
            if (IsPlaying)
            {
                throw new InvalidOperationException($"Timeline '{cutsceneId}' is already playing.");
            }

            if (playableDirector == null || playableDirector.playableAsset == null)
            {
                onFinished?.Invoke(CutsceneEndReason.Failed);
                return;
            }

            _onFinished = onFinished;
            _stopReason = CutsceneEndReason.Completed;
            playableDirector.stopped += HandleStopped;
            playableDirector.time = 0d;
            playableDirector.Play();
        }

        public void Skip()
        {
            if (!IsPlaying || !CanSkip)
            {
                return;
            }

            if (evaluateEndOnSkip && playableDirector.duration > 0d)
            {
                playableDirector.time = playableDirector.duration;
                playableDirector.Evaluate();
            }

            Stop(CutsceneEndReason.Skipped);
        }

        public void Stop(CutsceneEndReason reason)
        {
            if (!IsPlaying)
            {
                return;
            }

            _stopReason = reason;
            playableDirector.Stop();
            if (IsPlaying)
            {
                Finish(reason);
            }
        }

        private void HandleStopped(PlayableDirector director)
        {
            Finish(_stopReason);
        }

        private void Finish(CutsceneEndReason reason)
        {
            playableDirector.stopped -= HandleStopped;
            var callback = _onFinished;
            _onFinished = null;
            callback?.Invoke(reason);
        }
    }
}
