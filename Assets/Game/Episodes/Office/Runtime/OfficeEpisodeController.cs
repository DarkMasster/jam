using UnityEngine;
using UnityEngine.UI;

namespace Jam.Episodes.Office
{
    public sealed class OfficeEpisodeController : MonoBehaviour
    {
        private const string EmptyHandsMessage = "РУКИ СВОБОДНЫ • ПОДОЙДИ К ПОДСВЕЧЕННОМУ ПРЕДМЕТУ";

        [SerializeField] private Text zoneText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text carryText;
        [SerializeField] private Text statusText;
        [SerializeField] private OfficeExitGate exitGate;

        private int _breakableTotal;
        private int _breakableDestroyed;

        public bool HasLaptop { get; private set; }
        public bool HasMug { get; private set; }
        public bool BossEncounterReady => HasLaptop && HasMug;

        private void Awake()
        {
            if (zoneText == null || objectiveText == null || carryText == null || statusText == null || exitGate == null)
            {
                Debug.LogError($"{nameof(OfficeEpisodeController)} on '{name}' is missing a scene reference.", this);
            }

            RefreshHud();
            SetCarry(EmptyHandsMessage);
            EnterZone("СТАРТОВЫЙ КАБИНЕТ");
        }

        public void Configure(Text zone, Text objective, Text carry, Text status, OfficeExitGate gate)
        {
            zoneText = zone;
            objectiveText = objective;
            carryText = carry;
            statusText = status;
            exitGate = gate;
        }

        public void RegisterCollectible(OfficeCollectibleType collectibleType)
        {
            switch (collectibleType)
            {
                case OfficeCollectibleType.Laptop:
                    HasLaptop = true;
                    SetStatus("НОУТБУК СОБРАН • ОСТАЛАСЬ КРУЖКА");
                    break;
                case OfficeCollectibleType.Mug:
                    HasMug = true;
                    SetStatus("КРУЖКА СОБРАНА • ОСТАЛСЯ НОУТБУК");
                    break;
            }

            if (BossEncounterReady)
            {
                SetStatus("ЛИЧНЫЕ ВЕЩИ СОБРАНЫ • ДОБЕРИСЬ ДО EXIT");
            }

            RefreshHud();
            exitGate?.SetReady(BossEncounterReady);
        }

        public void EnterZone(string zoneName)
        {
            if (zoneText != null)
            {
                zoneText.text = zoneName;
            }
        }

        public void RegisterBreakableTarget()
        {
            _breakableTotal++;
        }

        public void RegisterBreakableDestroyed(string targetName)
        {
            _breakableDestroyed++;
            SetStatus($"РАЗРУШЕНО: {targetName} • {_breakableDestroyed}/{_breakableTotal}");
        }

        public void ReportCarryPickup(string itemName)
        {
            SetCarry($"В РУКАХ: {itemName} • PRIMARY — БРОСОК");
            SetStatus($"ПОДОБРАНО: {itemName} • РУКИ ЗАНЯТЫ");
        }

        public void ReportCarryThrow(string itemName)
        {
            SetCarry(EmptyHandsMessage);
            SetStatus($"БРОСОК: {itemName}");
        }

        public void HandleExitAttempt()
        {
            if (!BossEncounterReady)
            {
                var missing = !HasLaptop && !HasMug
                    ? "НУЖНЫ НОУТБУК И КРУЖКА"
                    : !HasLaptop
                        ? "НУЖЕН НОУТБУК"
                        : "НУЖНА КРУЖКА";

                SetStatus($"ДОСТУП ОТКЛОНЁН • {missing}");
                return;
            }

            if (objectiveText != null)
            {
                objectiveText.text = "EXIT — ЛОЖНАЯ ЦЕЛЬ • ВПЕРЕДИ СЕРВЕРНЫЙ БОСС";
            }

            SetStatus("ДОСТУП ОТОЗВАН • ЗОНА БОССА ГОТОВА ДЛЯ СЛЕДУЮЩЕГО СРЕЗА");
        }

        private void RefreshHud()
        {
            if (objectiveText == null)
            {
                return;
            }

            var laptop = HasLaptop ? "[X] НОУТБУК" : "[ ] НОУТБУК";
            var mug = HasMug ? "[X] КРУЖКА" : "[ ] КРУЖКА";
            objectiveText.text = $"СОБЕРИ ЛИЧНЫЕ ВЕЩИ   {laptop}   {mug}";
        }

        private void SetCarry(string message)
        {
            if (carryText != null)
            {
                carryText.text = message;
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
    }
}
