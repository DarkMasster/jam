using UnityEngine;
using UnityEngine.UI;

namespace Jam.Episodes.Office
{
    public sealed class OfficeEpisodeController : MonoBehaviour
    {
        private const string EmptyHandsMessage = "РУКИ СВОБОДНЫ • ПОДОЙДИ К ПОДСВЕЧЕННОМУ ПРЕДМЕТУ";
        private const string StartZoneName = "СТАРТОВЫЙ КАБИНЕТ";

        [Header("HUD")]
        [SerializeField] private Text zoneText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text carryText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text integrityText;
        [SerializeField] private Text momentumText;
        [SerializeField] private Image momentumFill;
        [SerializeField] private GameObject downPanel;
        [SerializeField] private Text downText;

        [Header("Сцена")]
        [SerializeField] private OfficeExitGate exitGate;
        [SerializeField] private OfficeMomentum momentum;

        [Header("Подача Momentum")]
        [SerializeField] private Color momentumLowColor = new(0.43f, 0.08f, 0.07f, 1f);
        [SerializeField] private Color momentumHighColor = new(1f, 0.35f, 0.24f, 1f);

        private int _breakableTotal;
        private int _breakableDestroyed;
        private int _chaserTotal;
        private int _chaserWrecked;

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
            EnterZone(StartZoneName);
            ShowDownPanel(false, string.Empty);
        }

        private void Update()
        {
            RefreshMomentumHud();
        }

        public void Configure(
            Text zone,
            Text objective,
            Text carry,
            Text status,
            Text integrity,
            Text momentumLabel,
            Image momentumBar,
            GameObject downOverlay,
            Text downMessage,
            OfficeExitGate gate,
            OfficeMomentum momentumScale)
        {
            zoneText = zone;
            objectiveText = objective;
            carryText = carry;
            statusText = status;
            integrityText = integrity;
            momentumText = momentumLabel;
            momentumFill = momentumBar;
            downPanel = downOverlay;
            downText = downMessage;
            exitGate = gate;
            momentum = momentumScale;
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
            SetStatus($"РАЗРУШЕНО: {targetName} • {_breakableDestroyed}/{_breakableTotal} • ТЕМП РАСТЁТ");
        }

        public void RegisterChaser()
        {
            _chaserTotal++;
        }

        public void RegisterChaserWrecked(string targetName)
        {
            _chaserWrecked++;
            SetStatus($"СПИСАНО: {targetName} • {_chaserWrecked}/{_chaserTotal} • ТЕМП РАСТЁТ");
        }

        public void ReportChaserCrash(string targetName)
        {
            SetStatus($"{targetName} ПРОМАХНУЛОСЬ • ОКНО ДЛЯ БРОСКА");
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

        public void ReportRunState(int integrity, int maxIntegrity, int attempt)
        {
            if (integrityText == null)
            {
                return;
            }

            var pips = string.Empty;
            for (var i = 0; i < maxIntegrity; i++)
            {
                pips += i < integrity ? "■" : "□";
            }

            integrityText.text = $"РАБОТОСПОСОБНОСТЬ {pips}   ПОПЫТКА {attempt}";
        }

        public void ReportPlayerHit(string sourceName, int integrity, int maxIntegrity)
        {
            SetStatus($"УДАР: {sourceName} • РАБОТОСПОСОБНОСТЬ {integrity}/{maxIntegrity}");
        }

        public void ReportRunFailed(string sourceName, int nextAttempt)
        {
            SetStatus($"ВЫГОРАНИЕ • ПРИЧИНА: {sourceName}");
            ShowDownPanel(true, $"ПРОИЗВОДИТЕЛЬНОСТЬ НЕУДОВЛЕТВОРИТЕЛЬНА\nПОПЫТКА {nextAttempt} НАЧИНАЕТСЯ");
        }

        public void ReportRunRestarted(int attempt)
        {
            ShowDownPanel(false, string.Empty);
            SetStatus($"РАБОЧИЙ ДЕНЬ ПЕРЕЗАПУЩЕН • ПОПЫТКА {attempt}");
        }

        /// <summary>Возвращает цели и счётчики забега в стартовое состояние.</summary>
        public void ResetForRun()
        {
            HasLaptop = false;
            HasMug = false;
            _breakableDestroyed = 0;
            _chaserWrecked = 0;

            RefreshHud();
            SetCarry(EmptyHandsMessage);
            EnterZone(StartZoneName);
            exitGate?.SetReady(false);
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

        private void RefreshMomentumHud()
        {
            if (momentum == null)
            {
                return;
            }

            var value = momentum.Value;

            if (momentumFill != null)
            {
                // Ширину задаём якорем, чтобы полоса не зависела от sprite и Image.Type.
                var rect = momentumFill.rectTransform;
                var anchorMax = rect.anchorMax;
                anchorMax.x = value;
                rect.anchorMax = anchorMax;
                momentumFill.color = Color.Lerp(momentumLowColor, momentumHighColor, value);
            }

            if (momentumText != null)
            {
                var state = momentum.IsIdle ? "ПРОСТОЙ" : "В ДВИЖЕНИИ";
                momentumText.text = $"ТЕМП {Mathf.RoundToInt(value * 100f)}%   {state}";
            }
        }

        private void ShowDownPanel(bool visible, string message)
        {
            if (downPanel != null)
            {
                downPanel.SetActive(visible);
            }

            if (downText != null && visible)
            {
                downText.text = message;
            }
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
