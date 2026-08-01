using UnityEngine;
using UnityEngine.UI;

namespace Jam.Episodes.Office
{
    public sealed class OfficeHudBinding : MonoBehaviour
    {
        [SerializeField] private Text zone;
        [SerializeField] private Text objective;
        [SerializeField] private Text carry;
        [SerializeField] private Text status;
        [SerializeField] private Text integrity;
        [SerializeField] private Text momentum;
        [SerializeField] private Image momentumFill;
        [SerializeField] private GameObject downPanel;
        [SerializeField] private Text downText;
        [SerializeField] private Text coach;

        public Text Zone => zone;
        public Text Objective => objective;
        public Text Carry => carry;
        public Text Status => status;
        public Text Integrity => integrity;
        public Text Momentum => momentum;
        public Image MomentumFill => momentumFill;
        public GameObject DownPanel => downPanel;
        public Text DownText => downText;

        public Text Coach => coach;

        public void Configure(
            Text zoneText,
            Text objectiveText,
            Text carryText,
            Text statusText,
            Text integrityText,
            Text momentumText,
            Image momentumBar,
            GameObject downOverlay,
            Text downMessage,
            Text coachText)
        {
            coach = coachText;
            zone = zoneText;
            objective = objectiveText;
            carry = carryText;
            status = statusText;
            integrity = integrityText;
            momentum = momentumText;
            momentumFill = momentumBar;
            downPanel = downOverlay;
            downText = downMessage;
        }
    }
}
