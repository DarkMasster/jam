using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Jam.Episodes.Office
{
    public sealed class OfficeHudBinding : MonoBehaviour
    {
        [SerializeField] private TMP_Text zone;
        [SerializeField] private TMP_Text objective;
        [SerializeField] private TMP_Text carry;
        [SerializeField] private TMP_Text status;
        [SerializeField] private TMP_Text integrity;
        [SerializeField] private TMP_Text momentum;
        [SerializeField] private Image momentumFill;
        [SerializeField] private GameObject downPanel;
        [SerializeField] private TMP_Text downText;
        [SerializeField] private TMP_Text coach;

        public TMP_Text Zone => zone;
        public TMP_Text Objective => objective;
        public TMP_Text Carry => carry;
        public TMP_Text Status => status;
        public TMP_Text Integrity => integrity;
        public TMP_Text Momentum => momentum;
        public Image MomentumFill => momentumFill;
        public GameObject DownPanel => downPanel;
        public TMP_Text DownText => downText;

        public TMP_Text Coach => coach;

        public void Configure(
            TMP_Text zoneText,
            TMP_Text objectiveText,
            TMP_Text carryText,
            TMP_Text statusText,
            TMP_Text integrityText,
            TMP_Text momentumText,
            Image momentumBar,
            GameObject downOverlay,
            TMP_Text downMessage,
            TMP_Text coachText)
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
