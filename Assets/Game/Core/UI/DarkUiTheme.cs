using UnityEngine;

namespace Jam.Core.UI
{
    [CreateAssetMenu(menuName = "Jam/UI/DarkUI Theme", fileName = "DarkUiTheme")]
    public sealed class DarkUiTheme : ScriptableObject
    {
        [SerializeField] private Sprite button;
        [SerializeField] private Sprite divider;

        public Sprite Button => button;
        public Sprite Divider => divider;

        public static DarkUiTheme Load()
        {
            return Resources.Load<DarkUiTheme>("UI/DarkUiTheme");
        }
    }
}
