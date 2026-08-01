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

        public Text Zone => zone;
        public Text Objective => objective;
        public Text Carry => carry;
        public Text Status => status;

        public void Configure(Text zoneText, Text objectiveText, Text carryText, Text statusText)
        {
            zone = zoneText;
            objective = objectiveText;
            carry = carryText;
            status = statusText;
        }
    }
}
