using UnityEngine;

namespace Jam.Core.UI
{
    public sealed class CharacterSelectPortraitRenderer : MonoBehaviour
    {
        [SerializeField] private Camera[] portraitCameras = new Camera[3];
        private readonly RenderTexture[] _textures = new RenderTexture[3];

        public Texture GetPortrait(int index)
        {
            if (index < 0 || index >= _textures.Length) return null;
            EnsureTextures();
            return _textures[index];
        }

        private void OnEnable()
        {
            EnsureTextures();
        }

        private void OnDisable()
        {
            for (var index = 0; index < _textures.Length; index++)
            {
                if (portraitCameras != null && index < portraitCameras.Length && portraitCameras[index] != null)
                {
                    portraitCameras[index].targetTexture = null;
                }

                if (_textures[index] == null) continue;
                _textures[index].Release();
                Destroy(_textures[index]);
                _textures[index] = null;
            }
        }

        private void EnsureTextures()
        {
            for (var index = 0; index < _textures.Length; index++)
            {
                if (portraitCameras == null || index >= portraitCameras.Length || portraitCameras[index] == null) continue;
                if (_textures[index] == null)
                {
                    _textures[index] = new RenderTexture(384, 384, 24, RenderTextureFormat.ARGB32)
                    {
                        name = $"CharacterSelectPortrait_{index}",
                        antiAliasing = 2,
                        useMipMap = false
                    };
                    _textures[index].Create();
                }

                portraitCameras[index].targetTexture = _textures[index];
            }
        }
    }
}
