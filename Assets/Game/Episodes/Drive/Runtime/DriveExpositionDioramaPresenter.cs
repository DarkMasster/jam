using Jam.Core.Cutscenes;
using Unity.Cinemachine;
using UnityEngine;

namespace Jam.Episodes.Drive
{
    public sealed class DriveExpositionDioramaPresenter : MonoBehaviour, IStoryboardScenePresenter
    {
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Camera stageCamera;
        [SerializeField] private Camera portraitCamera;
        [SerializeField] private CinemachineCamera[] stageShots;

        private RenderTexture _stageTexture;
        private RenderTexture _portraitTexture;

        public Texture StageTexture { get { EnsureTextures(); return _stageTexture; } }
        public Texture PortraitTexture { get { EnsureTextures(); return _portraitTexture; } }

        public void ShowFrame(int frameIndex)
        {
            EnsureTextures();
            if (visualRoot != null) visualRoot.SetActive(true);
            if (stageCamera != null) stageCamera.enabled = true;
            if (portraitCamera != null) portraitCamera.enabled = true;
            if (stageShots == null) return;
            var selected = Mathf.Clamp(frameIndex, 0, stageShots.Length - 1);
            for (var index = 0; index < stageShots.Length; index++)
            {
                if (stageShots[index] != null) stageShots[index].gameObject.SetActive(index == selected);
            }
        }

        public void Hide()
        {
            if (visualRoot != null) visualRoot.SetActive(false);
            if (stageCamera != null) stageCamera.enabled = false;
            if (portraitCamera != null) portraitCamera.enabled = false;
        }

        private void OnDisable()
        {
            Release(ref _stageTexture, stageCamera);
            Release(ref _portraitTexture, portraitCamera);
        }

        private void EnsureTextures()
        {
            Ensure(ref _stageTexture, stageCamera, 1280, 720, "DriveExpositionStageRT");
            Ensure(ref _portraitTexture, portraitCamera, 512, 512, "DriveExpositionPortraitRT");
        }

        private static void Ensure(ref RenderTexture texture, Camera camera, int width, int height, string name)
        {
            if (camera == null) return;
            if (texture == null)
            {
                texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { name = name, antiAliasing = 2 };
                texture.Create();
            }
            camera.targetTexture = texture;
        }

        private static void Release(ref RenderTexture texture, Camera camera)
        {
            if (camera != null) camera.targetTexture = null;
            if (texture == null) return;
            texture.Release();
            Destroy(texture);
            texture = null;
        }
    }
}
