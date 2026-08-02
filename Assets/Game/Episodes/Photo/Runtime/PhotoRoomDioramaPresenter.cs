using UnityEngine;
using Unity.Cinemachine;

namespace Jam.Episodes.Photo
{
    public sealed class PhotoRoomDioramaPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private GameObject entranceRoot;
        [SerializeField] private GameObject airportRoot;
        [SerializeField] private Camera stageCamera;
        [SerializeField] private GameObject portraitRoot;
        [SerializeField] private Camera portraitCamera;
        [SerializeField] private GameObject heroinePortrait;
        [SerializeField] private GameObject motherPortrait;
        [SerializeField] private GameObject officerPortrait;
        [SerializeField] private CinemachineCamera wideCamera;
        [SerializeField] private CinemachineCamera photoCamera;
        [SerializeField] private CinemachineCamera dialogueCamera;
        [SerializeField] private CinemachineCamera entranceWideCamera;
        [SerializeField] private CinemachineCamera entrancePhotoCamera;
        [SerializeField] private CinemachineCamera entranceReactionCamera;
        [SerializeField] private CinemachineCamera airportPhotoCamera;
        [SerializeField] private CinemachineCamera borderControlCamera;
        [SerializeField] private CinemachineCamera airportSummaryCamera;
        [SerializeField] private CinemachineCamera heroinePortraitCamera;
        [SerializeField] private CinemachineCamera motherPortraitCamera;
        [SerializeField] private CinemachineCamera officerPortraitCamera;

        private RenderTexture _output;
        private RenderTexture _portraitOutput;

        public Texture OutputTexture
        {
            get
            {
                EnsureOutput();
                return _output;
            }
        }

        public Texture PortraitTexture
        {
            get
            {
                EnsureOutput();
                return _portraitOutput;
            }
        }

        public bool IsSupportedStep(PhotoPrologueStep step)
        {
            return step == PhotoPrologueStep.RoomSecret
                   || step == PhotoPrologueStep.RoomPhoto
                   || step == PhotoPrologueStep.MotherDialogue
                   || step == PhotoPrologueStep.MailboxHunt
                   || step == PhotoPrologueStep.MailboxPublication
                   || step == PhotoPrologueStep.MailboxReaction
                   || step == PhotoPrologueStep.AirportPhoto
                   || step == PhotoPrologueStep.BorderControl
                   || step == PhotoPrologueStep.Summary;
        }

        public bool Present(PhotoPrologueStep step)
        {
            if (!IsSupportedStep(step) || stageCamera == null || visualRoot == null)
            {
                Hide();
                return false;
            }

            EnsureOutput();
            var entranceStep = step == PhotoPrologueStep.MailboxHunt
                               || step == PhotoPrologueStep.MailboxPublication
                               || step == PhotoPrologueStep.MailboxReaction;
            var airportStep = step == PhotoPrologueStep.AirportPhoto
                              || step == PhotoPrologueStep.BorderControl
                              || step == PhotoPrologueStep.Summary;
            visualRoot.SetActive(!entranceStep && !airportStep);
            if (entranceRoot != null) entranceRoot.SetActive(entranceStep);
            if (airportRoot != null) airportRoot.SetActive(airportStep);
            stageCamera.enabled = true;
            if (portraitRoot != null) portraitRoot.SetActive(true);
            if (portraitCamera != null) portraitCamera.enabled = true;
            var showMother = step == PhotoPrologueStep.MotherDialogue;
            var showOfficer = step == PhotoPrologueStep.BorderControl;
            if (heroinePortrait != null) heroinePortrait.SetActive(!showMother && !showOfficer);
            if (motherPortrait != null) motherPortrait.SetActive(showMother);
            if (officerPortrait != null) officerPortrait.SetActive(showOfficer);
            if (heroinePortraitCamera != null) heroinePortraitCamera.gameObject.SetActive(!showMother && !showOfficer);
            if (motherPortraitCamera != null) motherPortraitCamera.gameObject.SetActive(showMother);
            if (officerPortraitCamera != null) officerPortraitCamera.gameObject.SetActive(showOfficer);
            switch (step)
            {
                case PhotoPrologueStep.RoomPhoto:
                    SelectStageCamera(photoCamera);
                    break;
                case PhotoPrologueStep.MotherDialogue:
                    SelectStageCamera(dialogueCamera);
                    break;
                case PhotoPrologueStep.MailboxHunt:
                    SelectStageCamera(entranceWideCamera);
                    break;
                case PhotoPrologueStep.MailboxPublication:
                    SelectStageCamera(entrancePhotoCamera);
                    break;
                case PhotoPrologueStep.MailboxReaction:
                    SelectStageCamera(entranceReactionCamera);
                    break;
                case PhotoPrologueStep.AirportPhoto:
                    SelectStageCamera(airportPhotoCamera);
                    break;
                case PhotoPrologueStep.BorderControl:
                    SelectStageCamera(borderControlCamera);
                    break;
                case PhotoPrologueStep.Summary:
                    SelectStageCamera(airportSummaryCamera);
                    break;
                default:
                    SelectStageCamera(wideCamera);
                    break;
            }

            return true;
        }

        public void Hide()
        {
            if (visualRoot != null) visualRoot.SetActive(false);
            if (entranceRoot != null) entranceRoot.SetActive(false);
            if (airportRoot != null) airportRoot.SetActive(false);
            if (stageCamera != null) stageCamera.enabled = false;
            if (portraitRoot != null) portraitRoot.SetActive(false);
            if (portraitCamera != null) portraitCamera.enabled = false;
            SetStageCamerasActive(null);
            if (heroinePortraitCamera != null) heroinePortraitCamera.gameObject.SetActive(false);
            if (motherPortraitCamera != null) motherPortraitCamera.gameObject.SetActive(false);
            if (officerPortraitCamera != null) officerPortraitCamera.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (stageCamera != null) stageCamera.targetTexture = null;
            if (portraitCamera != null) portraitCamera.targetTexture = null;
            if (_output != null)
            {
                _output.Release();
                Destroy(_output);
                _output = null;
            }

            if (_portraitOutput != null)
            {
                _portraitOutput.Release();
                Destroy(_portraitOutput);
                _portraitOutput = null;
            }
        }

        private void EnsureOutput()
        {
            if (_output == null)
            {
                _output = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32)
                {
                    name = "PhotoRoomDioramaRT",
                    antiAliasing = 2,
                    useMipMap = false
                };
                _output.Create();
            }

            if (stageCamera != null) stageCamera.targetTexture = _output;

            if (_portraitOutput == null)
            {
                _portraitOutput = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32)
                {
                    name = "PhotoDialoguePortraitRT",
                    antiAliasing = 2,
                    useMipMap = false
                };
                _portraitOutput.Create();
            }

            if (portraitCamera != null) portraitCamera.targetTexture = _portraitOutput;
        }

        private void SelectStageCamera(CinemachineCamera selected)
        {
            SetStageCamerasActive(selected);
        }

        private void SetStageCamerasActive(CinemachineCamera selected)
        {
            SetCameraActive(wideCamera, selected);
            SetCameraActive(photoCamera, selected);
            SetCameraActive(dialogueCamera, selected);
            SetCameraActive(entranceWideCamera, selected);
            SetCameraActive(entrancePhotoCamera, selected);
            SetCameraActive(entranceReactionCamera, selected);
            SetCameraActive(airportPhotoCamera, selected);
            SetCameraActive(borderControlCamera, selected);
            SetCameraActive(airportSummaryCamera, selected);
        }

        private static void SetCameraActive(CinemachineCamera camera, CinemachineCamera selected)
        {
            if (camera != null) camera.gameObject.SetActive(camera == selected);
        }
    }
}
