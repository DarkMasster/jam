using System;
using UnityEngine;

namespace Jam.Core.Cutscenes
{
    [Serializable]
    public sealed class CutsceneContext
    {
        public string characterId = string.Empty;
        public string startCheckpointId = string.Empty;
        public string completionCheckpointId = string.Empty;
    }

    public enum CutsceneEndReason
    {
        Completed,
        Skipped,
        Failed,
        SceneChanged
    }

    public readonly struct CutsceneResult
    {
        public CutsceneResult(string cutsceneId, CutsceneEndReason reason, CutsceneContext context)
        {
            CutsceneId = cutsceneId;
            Reason = reason;
            Context = context;
        }

        public string CutsceneId { get; }
        public CutsceneEndReason Reason { get; }
        public CutsceneContext Context { get; }
        public bool Succeeded => Reason is CutsceneEndReason.Completed or CutsceneEndReason.Skipped;
    }

    public interface ICutscenePresentation
    {
        string CutsceneId { get; }
        bool IsPlaying { get; }
        bool CanSkip { get; }
        void Play(CutsceneContext context, Action<CutsceneEndReason> onFinished);
        void Skip();
        void Stop(CutsceneEndReason reason);
    }

    public interface IStoryboardScenePresenter
    {
        Texture StageTexture { get; }
        Texture PortraitTexture { get; }
        void ShowFrame(int frameIndex);
        void Hide();
    }
}
