using Jam.Core.Cutscenes;
using Unity.Cinemachine;
using UnityEngine;

namespace Jam.Episodes.Office
{
    public sealed class OfficeStoryboardScenePresenter : MonoBehaviour, IStoryboardScenePresenter
    {
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Camera stageCamera;
        [SerializeField] private Camera portraitCamera;
        [SerializeField] private CinemachineCamera[] shots;
        private RenderTexture _stage;
        private RenderTexture _portrait;
        public Texture StageTexture { get { Ensure(); return _stage; } }
        public Texture PortraitTexture { get { Ensure(); return _portrait; } }

        public void ShowFrame(int frameIndex)
        {
            Ensure(); if (visualRoot != null) visualRoot.SetActive(true); if (stageCamera != null) stageCamera.enabled = true; if (portraitCamera != null) portraitCamera.enabled = true;
            for (var i = 0; shots != null && i < shots.Length; i++) if (shots[i] != null) shots[i].gameObject.SetActive(i == Mathf.Clamp(frameIndex, 0, shots.Length - 1));
        }
        public void Hide() { if (visualRoot != null) visualRoot.SetActive(false); if (stageCamera != null) stageCamera.enabled = false; if (portraitCamera != null) portraitCamera.enabled = false; }
        private void OnDisable() { Release(ref _stage, stageCamera); Release(ref _portrait, portraitCamera); }
        private void Ensure() { EnsureOne(ref _stage, stageCamera, 1280, 720, "OfficeStoryboardStageRT"); EnsureOne(ref _portrait, portraitCamera, 512, 512, "OfficeStoryboardPortraitRT"); }
        private static void EnsureOne(ref RenderTexture rt, Camera camera, int w, int h, string n) { if (camera == null) return; if (rt == null) { rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { name = n, antiAliasing = 2 }; rt.Create(); } camera.targetTexture = rt; }
        private static void Release(ref RenderTexture rt, Camera camera) { if (camera != null) camera.targetTexture = null; if (rt == null) return; rt.Release(); Destroy(rt); rt = null; }
    }
}
