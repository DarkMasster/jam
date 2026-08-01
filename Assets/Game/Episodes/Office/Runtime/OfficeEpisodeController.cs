using Jam.Core.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Jam.Episodes.Office
{
    public sealed class OfficeEpisodeController : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private TMP_Text zoneText;
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text carryText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text integrityText;
        [SerializeField] private TMP_Text momentumText;
        [SerializeField] private Image momentumFill;
        [SerializeField] private GameObject downPanel;
        [SerializeField] private TMP_Text downText;

        [Header("Сцена")]
        [SerializeField] private OfficeExitGate exitGate;
        [SerializeField] private OfficeMomentum momentum;
        [SerializeField] private OfficeBossEncounter bossEncounter;

        [Header("Подача Momentum")]
        [SerializeField] private Color momentumLowColor = new(0.43f, 0.08f, 0.07f, 1f);
        [SerializeField] private Color momentumHighColor = new(1f, 0.35f, 0.24f, 1f);

        private int _breakableTotal;
        private int _breakableDestroyed;
        private int _chaserTotal;
        private int _chaserWrecked;
        private int _integrity = 3;
        private int _maxIntegrity = 3;
        private int _attempt = 1;
        private OfficeBossPhase _bossPhase = OfficeBossPhase.Dormant;
        private int _bossHits;
        private int _bossRequiredHits;

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
            SetCarry(Loc.Get(LocalizationTables.Office, "hud.empty_hands", "РУКИ СВОБОДНЫ • ПОДОЙДИ К ПОДСВЕЧЕННОМУ ПРЕДМЕТУ"));
            EnterZone(Loc.Get(LocalizationTables.Office, "zone.start", "СТАРТОВЫЙ КАБИНЕТ"));
            SetStatus(Loc.Get(LocalizationTables.Office, "hud.start_hint", "WASD / СТРЕЛКИ • ДВИГАЙСЯ К EXIT"));
            ReportRunState(3, 3, 1);
            ShowDownPanel(false, Loc.Get(LocalizationTables.Office, "hud.down", "ПРОИЗВОДИТЕЛЬНОСТЬ НЕУДОВЛЕТВОРИТЕЛЬНА\nПОПЫТКА {0} НАЧИНАЕТСЯ", 1));
        }

        private void OnEnable()
        {
            Loc.LocaleChanged += HandleLocaleChanged;
        }

        private void OnDisable()
        {
            Loc.LocaleChanged -= HandleLocaleChanged;
        }

        private void HandleLocaleChanged()
        {
            RefreshHud();
            RefreshMomentumHud();
            ReportRunState(_integrity, _maxIntegrity, _attempt);
        }

        private void Update()
        {
            RefreshMomentumHud();
        }

        public void Configure(
            TMP_Text zone,
            TMP_Text objective,
            TMP_Text carry,
            TMP_Text status,
            TMP_Text integrity,
            TMP_Text momentumLabel,
            Image momentumBar,
            GameObject downOverlay,
            TMP_Text downMessage,
            OfficeExitGate gate,
            OfficeMomentum momentumScale,
            OfficeBossEncounter boss)
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
            bossEncounter = boss;
        }

        public void RegisterCollectible(OfficeCollectibleType collectibleType)
        {
            switch (collectibleType)
            {
                case OfficeCollectibleType.Laptop:
                    HasLaptop = true;
                    SetStatus(Loc.Get(LocalizationTables.Office, "status.laptop_collected", "НОУТБУК СОБРАН • ОСТАЛАСЬ КРУЖКА"));
                    break;
                case OfficeCollectibleType.Mug:
                    HasMug = true;
                    SetStatus(Loc.Get(LocalizationTables.Office, "status.mug_collected", "КРУЖКА СОБРАНА • ОСТАЛСЯ НОУТБУК"));
                    break;
            }

            if (BossEncounterReady)
            {
                SetStatus(Loc.Get(LocalizationTables.Office, "status.items_collected", "ЛИЧНЫЕ ВЕЩИ СОБРАНЫ • ДОБЕРИСЬ ДО EXIT"));
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
            SetStatus(Loc.Get(LocalizationTables.Office, "status.destroyed", "РАЗРУШЕНО: {0} • {1}/{2} • ТЕМП РАСТЁТ", LocalizeRuntimeName(targetName), _breakableDestroyed, _breakableTotal));
        }

        public void RegisterChaser()
        {
            _chaserTotal++;
        }

        public void RegisterChaserWrecked(string targetName)
        {
            _chaserWrecked++;
            SetStatus(Loc.Get(LocalizationTables.Office, "status.wrecked", "СПИСАНО: {0} • {1}/{2} • ТЕМП РАСТЁТ", LocalizeRuntimeName(targetName), _chaserWrecked, _chaserTotal));
        }

        public void ReportChaserCrash(string targetName)
        {
            SetStatus(Loc.Get(LocalizationTables.Office, "status.chaser_missed", "{0} ПРОМАХНУЛОСЬ • ОКНО ДЛЯ БРОСКА", LocalizeRuntimeName(targetName)));
        }

        /// <summary>Короткая сюжетная или служебная строка маршрута.</summary>
        public void ReportStoryBeat(string message)
        {
            SetStatus(message);
        }

        public void ReportCarryPickup(string itemName)
        {
            var localizedName = LocalizeRuntimeName(itemName);
            SetCarry(Loc.Get(LocalizationTables.Office, "hud.carrying", "В РУКАХ: {0} • PRIMARY — БРОСОК", localizedName));
            SetStatus(Loc.Get(LocalizationTables.Office, "status.picked_up", "ПОДОБРАНО: {0} • РУКИ ЗАНЯТЫ", localizedName));
        }

        public void ReportCarryThrow(string itemName)
        {
            SetCarry(Loc.Get(LocalizationTables.Office, "hud.empty_hands", "РУКИ СВОБОДНЫ • ПОДОЙДИ К ПОДСВЕЧЕННОМУ ПРЕДМЕТУ"));
            SetStatus(Loc.Get(LocalizationTables.Office, "status.thrown", "БРОСОК: {0}", LocalizeRuntimeName(itemName)));
        }

        public void ReportRunState(int integrity, int maxIntegrity, int attempt)
        {
            _integrity = integrity;
            _maxIntegrity = maxIntegrity;
            _attempt = attempt;

            if (integrityText == null)
            {
                return;
            }

            var pips = string.Empty;
            for (var i = 0; i < maxIntegrity; i++)
            {
                pips += i < integrity ? "■" : "□";
            }

            integrityText.text = Loc.Get(LocalizationTables.Office, "hud.integrity", "РАБОТОСПОСОБНОСТЬ {0}   ПОПЫТКА {1}", pips, attempt);
        }

        public void ReportPlayerHit(string sourceName, int integrity, int maxIntegrity)
        {
            SetStatus(Loc.Get(LocalizationTables.Office, "status.hit", "УДАР: {0} • РАБОТОСПОСОБНОСТЬ {1}/{2}", LocalizeRuntimeName(sourceName), integrity, maxIntegrity));
        }

        public void ReportRunFailed(string sourceName, int nextAttempt)
        {
            SetStatus(Loc.Get(LocalizationTables.Office, "status.failed", "ВЫГОРАНИЕ • ПРИЧИНА: {0}", LocalizeRuntimeName(sourceName)));
            ShowDownPanel(true, Loc.Get(LocalizationTables.Office, "hud.down", "ПРОИЗВОДИТЕЛЬНОСТЬ НЕУДОВЛЕТВОРИТЕЛЬНА\nПОПЫТКА {0} НАЧИНАЕТСЯ", nextAttempt));
        }

        public void ReportRunRestarted(int attempt)
        {
            ShowDownPanel(false, string.Empty);
            SetStatus(Loc.Get(LocalizationTables.Office, "status.restarted", "РАБОЧИЙ ДЕНЬ ПЕРЕЗАПУЩЕН • ПОПЫТКА {0}", attempt));
        }

        /// <summary>Возвращает цели и счётчики забега в стартовое состояние.</summary>
        public void ResetForRun()
        {
            HasLaptop = false;
            HasMug = false;
            _breakableDestroyed = 0;
            _chaserWrecked = 0;
            _bossPhase = OfficeBossPhase.Dormant;
            _bossHits = 0;
            _bossRequiredHits = 0;

            RefreshHud();
            SetCarry(Loc.Get(LocalizationTables.Office, "hud.empty_hands", "РУКИ СВОБОДНЫ • ПОДОЙДИ К ПОДСВЕЧЕННОМУ ПРЕДМЕТУ"));
            EnterZone(Loc.Get(LocalizationTables.Office, "zone.start", "СТАРТОВЫЙ КАБИНЕТ"));
            exitGate?.SetReady(false);
        }

        public void ReportBossAssemblyStarted()
        {
            _bossPhase = OfficeBossPhase.Assembling;
            RefreshHud();
            SetStatus(Loc.Get(LocalizationTables.Office, "boss.status.assembly", "СТОЙКИ СХОДЯТСЯ • УПРАВЛЕНИЕ ЗАБЛОКИРОВАНО"));
        }

        public void ReportBossAssembled(int requiredHits)
        {
            _bossPhase = OfficeBossPhase.Assembled;
            _bossHits = 0;
            _bossRequiredHits = requiredHits;
            RefreshHud();
            SetStatus(Loc.Get(LocalizationTables.Office, "boss.status.assembled", "ЕДИНЫЙ КОРПУС СОБРАН • БРОСАЙ ПРЕДМЕТЫ"));
        }

        public void ReportBossHit(int hits, int requiredHits)
        {
            _bossHits = hits;
            _bossRequiredHits = requiredHits;
            RefreshHud();
            SetStatus(Loc.Get(LocalizationTables.Office, "boss.status.hit", "СБОЙ КОРПУСА • {0}/{1}", hits, requiredHits));
        }

        public void ReportBossEncirclementStarted()
        {
            _bossPhase = OfficeBossPhase.Encircling;
            RefreshHud();
            SetStatus(Loc.Get(LocalizationTables.Office, "boss.status.encircling", "ЛОЖНАЯ ПОБЕДА • СТОЙКИ ПЕРЕСТРАИВАЮТСЯ"));
        }

        public void ReportBossRingClosed()
        {
            _bossPhase = OfficeBossPhase.RingFight;
            RefreshHud();
            SetStatus(Loc.Get(LocalizationTables.Office, "boss.status.ring", "КОЛЬЦО ЗАМКНУТО • ДВИГАЙСЯ, ПОКА ЕЩЁ МОЖЕШЬ"));
        }

        public void ReportBossFinalTelegraph()
        {
            _bossPhase = OfficeBossPhase.FinalTelegraph;
            RefreshHud();
            SetStatus(Loc.Get(LocalizationTables.Office, "boss.status.final", "СИНХРОНИЗАЦИЯ СТОЕК • БЕЗОПАСНОЙ ЗОНЫ НЕТ"));
        }

        public void ReportBossFinalStrike()
        {
            _bossPhase = OfficeBossPhase.Completed;
            RefreshHud();
            SetStatus(Loc.Get(LocalizationTables.Office, "boss.status.complete", "СЮЖЕТНОЕ ПОРАЖЕНИЕ • ЭПИЗОД ЗАВЕРШЁН"));
            ShowDownPanel(true, Loc.Get(LocalizationTables.Office, "boss.overlay.complete", "OFFBOARDING ЗАВЕРШЁН\nСОН ОБРЫВАЕТСЯ"));
        }

        public void HandleExitAttempt()
        {
            if (!BossEncounterReady)
            {
                var missingKey = !HasLaptop && !HasMug
                    ? "status.missing_both"
                    : !HasLaptop ? "status.missing_laptop" : "status.missing_mug";
                var missing = Loc.Get(LocalizationTables.Office, missingKey, "НУЖНЫ ЛИЧНЫЕ ВЕЩИ");
                SetStatus(Loc.Get(LocalizationTables.Office, "status.access_denied", "ДОСТУП ОТКЛОНЁН • {0}", missing));
                return;
            }

            if (objectiveText != null)
            {
                objectiveText.text = Loc.Get(LocalizationTables.Office, "objective.false_exit", "EXIT — ЛОЖНАЯ ЦЕЛЬ • ВПЕРЕДИ СЕРВЕРНЫЙ БОСС");
            }

            if (bossEncounter == null)
            {
                SetStatus(Loc.Get(LocalizationTables.Office, "status.access_revoked", "ДОСТУП ОТОЗВАН • СЕРВЕРНЫЙ БОСС НЕ ПОДКЛЮЧЁН"));
                return;
            }

            bossEncounter.TryStartEncounter();
        }

        private void RefreshHud()
        {
            if (objectiveText == null)
            {
                return;
            }

            if (_bossPhase != OfficeBossPhase.Dormant)
            {
                objectiveText.text = _bossPhase switch
                {
                    OfficeBossPhase.Assembling => Loc.Get(LocalizationTables.Office, "boss.objective.assembly", "СБОРКА КОРПУСА • НЕ ДВИГАТЬСЯ"),
                    OfficeBossPhase.Assembled => Loc.Get(LocalizationTables.Office, "boss.objective.assembled", "РАЗРУШЬ ЕДИНЫЙ КОРПУС   {0}/{1}", _bossHits, _bossRequiredHits),
                    OfficeBossPhase.Encircling => Loc.Get(LocalizationTables.Office, "boss.objective.encircling", "ЛОЖНАЯ ПОБЕДА • СТОЙКИ ОКРУЖАЮТ ТЕБЯ"),
                    OfficeBossPhase.RingFight => Loc.Get(LocalizationTables.Office, "boss.objective.ring", "СЕРВЕРНОЕ КОЛЬЦО • ВЫХОДА НЕТ"),
                    OfficeBossPhase.FinalTelegraph => Loc.Get(LocalizationTables.Office, "boss.objective.final", "ФИНАЛЬНЫЙ УДАР • ОБЩИЙ ТЕЛЕГРАФ"),
                    OfficeBossPhase.Completed => Loc.Get(LocalizationTables.Office, "boss.objective.complete", "OFFBOARDING ЗАВЕРШЁН • СОН ОБОРВАН"),
                    _ => string.Empty
                };
                return;
            }

            var laptop = HasLaptop
                ? Loc.Get(LocalizationTables.Office, "objective.laptop.done", "[X] НОУТБУК")
                : Loc.Get(LocalizationTables.Office, "objective.laptop.empty", "[ ] НОУТБУК");
            var mug = HasMug
                ? Loc.Get(LocalizationTables.Office, "objective.mug.done", "[X] КРУЖКА")
                : Loc.Get(LocalizationTables.Office, "objective.mug.empty", "[ ] КРУЖКА");
            objectiveText.text = Loc.Get(
                LocalizationTables.Office,
                "objective.collect",
                "СОБЕРИ ЛИЧНЫЕ ВЕЩИ   {0}   {1}",
                laptop,
                mug);
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
                var state = momentum.IsIdle
                    ? Loc.Get(LocalizationTables.Office, "momentum.idle", "ПРОСТОЙ")
                    : Loc.Get(LocalizationTables.Office, "momentum.moving", "В ДВИЖЕНИИ");
                momentumText.text = Loc.Get(
                    LocalizationTables.Office,
                    "momentum.label",
                    "ТЕМП {0}%   {1}",
                    Mathf.RoundToInt(value * 100f),
                    state);
            }
        }

        private void ShowDownPanel(bool visible, string message)
        {
            if (downPanel != null)
            {
                downPanel.SetActive(visible);
            }

            if (downText != null && !string.IsNullOrEmpty(message))
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

        private static string LocalizeRuntimeName(string value)
        {
            return value switch
            {
                "КЛАВИАТУРА" => Loc.Get(LocalizationTables.Office, "item.keyboard", value),
                "ПРИНТЕР" => Loc.Get(LocalizationTables.Office, "item.printer", value),
                "ОФИСНОЕ КРЕСЛО" => Loc.Get(LocalizationTables.Office, "enemy.chair", value),
                _ => value
            };
        }
    }
}
