using Jam.Core.Cutscenes;
using UnityEngine;

namespace Jam.Episodes.Photo
{
    public sealed class PhotoStoryboardScenePresenter : MonoBehaviour, IStoryboardScenePresenter
    {
        public enum StoryboardMode { Intro, Outro }
        [SerializeField] private PhotoRoomDioramaPresenter diorama;
        [SerializeField] private StoryboardMode mode;

        public Texture StageTexture => diorama != null ? diorama.OutputTexture : null;
        public Texture PortraitTexture => diorama != null ? diorama.PortraitTexture : null;

        public void ShowFrame(int frameIndex)
        {
            if (diorama == null) return;
            var intro = new[] { PhotoPrologueStep.RoomSecret, PhotoPrologueStep.RoomPhoto, PhotoPrologueStep.MotherDialogue, PhotoPrologueStep.RoomPhoto };
            var outro = new[] { PhotoPrologueStep.AirportPhoto, PhotoPrologueStep.BorderControl, PhotoPrologueStep.Summary, PhotoPrologueStep.Summary };
            var frames = mode == StoryboardMode.Intro ? intro : outro;
            diorama.Present(frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)]);
        }

        public void Hide()
        {
            diorama?.Hide();
        }

        public void Configure(PhotoRoomDioramaPresenter source, StoryboardMode storyboardMode)
        {
            diorama = source;
            mode = storyboardMode;
        }
    }
}
