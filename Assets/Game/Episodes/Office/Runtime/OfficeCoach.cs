using Jam.Core.Localization;
using TMPro;
using UnityEngine;

namespace Jam.Episodes.Office
{
    /// <summary>
    /// Обучение маршрута без tutorial-экрана: в HUD живёт одна подсказка за раз, а
    /// следующий шаг открывается только после того, как игрок выполнил предыдущий.
    /// Компонент ничего не вызывает у остальных систем и только наблюдает их состояние.
    /// </summary>
    public sealed class OfficeCoach : MonoBehaviour, IOfficeRunResettable
    {
        private const string MoveMessage = "WASD / СТРЕЛКИ — ИДИ ВПЕРЁД";
        private const string ApproachMessage = "ПРЕДМЕТ БЕРЁТСЯ САМ — ПОДОЙДИ К ПОДСВЕЧЕННОМУ";
        private const string ThrowMessage = "PRIMARY — БРОСЬ ПРЕДМЕТ В ТЕХНИКУ";
        private const string DodgeMessage = "КРАСНАЯ ПОЛОСА — СОЙДИ С ЛИНИИ РЫВКА";
        private const string CollectMessage = "СОБЕРИ НОУТБУК И КРУЖКУ И ИДИ К EXIT";
        private const string ExitMessage = "ЛИЧНЫЕ ВЕЩИ У ТЕБЯ — EXIT ПРЯМО ПО КОРИДОРУ";

        private enum CoachStep
        {
            Move,
            Approach,
            Throw,
            Dodge,
            Collect,
            Exit
        }

        [Header("HUD")]
        [SerializeField] private TMP_Text hintText;

        [Header("Сцена")]
        [SerializeField] private Transform player;
        [SerializeField] private OfficeCarryController carry;
        [SerializeField] private OfficeRunController run;
        [SerializeField] private OfficeEpisodeController episode;
        [SerializeField] private OfficeChaser[] chasers = new OfficeChaser[0];

        [Header("Условия шагов")]
        [SerializeField, Min(0.5f)] private float moveHintDistance = 1.25f;

        private CoachStep _step = CoachStep.Move;
        private Vector3 _lastPlayerPosition;
        private float _travelled;
        private bool _wasHolding;
        private bool _wasDashing;
        private int _integrityAtDashStart;
        private string _beatMessage;
        private float _beatUntil;

        private void OnEnable()
        {
            OfficeRunReset.Register(this);
            if (player != null)
            {
                _lastPlayerPosition = player.position;
            }
        }

        private void OnDisable()
        {
            OfficeRunReset.Unregister(this);
        }

        private void Update()
        {
            if (player == null || hintText == null)
            {
                return;
            }

            TrackTravel();
            AdvanceStep();
            hintText.text = ResolveMessage();
        }

        public void Configure(
            TMP_Text hint,
            Transform playerTransform,
            OfficeCarryController carryController,
            OfficeRunController runController,
            OfficeEpisodeController episodeController,
            OfficeChaser[] routeChasers)
        {
            hintText = hint;
            player = playerTransform;
            carry = carryController;
            run = runController;
            episode = episodeController;
            chasers = routeChasers ?? new OfficeChaser[0];
        }

        /// <summary>
        /// Показывает сюжетную или служебную строку поверх текущей подсказки.
        /// Используется Reflection beat и гарантией личных вещей.
        /// </summary>
        public void ShowBeat(string message, float duration)
        {
            _beatMessage = message;
            _beatUntil = Time.time + Mathf.Max(0.5f, duration);
        }

        public void ResetForRun()
        {
            // Выученные шаги не повторяются: после restart подсказка возвращается
            // только к цели забега, а управление и бросок игрок уже знает.
            if (_step == CoachStep.Exit)
            {
                _step = CoachStep.Collect;
            }

            _travelled = 0f;
            _wasHolding = false;
            _wasDashing = false;
            _beatMessage = string.Empty;
            _beatUntil = 0f;

            if (player != null)
            {
                _lastPlayerPosition = player.position;
            }
        }

        private void TrackTravel()
        {
            var position = player.position;
            var offset = position - _lastPlayerPosition;
            offset.y = 0f;
            _travelled += offset.magnitude;
            _lastPlayerPosition = position;
        }

        private void AdvanceStep()
        {
            var isHolding = carry != null && carry.HeldItem != null;

            switch (_step)
            {
                case CoachStep.Move:
                    if (_travelled >= moveHintDistance)
                    {
                        _step = CoachStep.Approach;
                    }

                    break;
                case CoachStep.Approach:
                    if (isHolding)
                    {
                        _step = CoachStep.Throw;
                    }

                    break;
                case CoachStep.Throw:
                    // Бросок виден как переход «предмет в руках → руки свободны».
                    // Мягкий restart роняет предмет, поэтому шаг засчитывается
                    // только внутри активного забега.
                    if (_wasHolding && !isHolding && (run == null || run.IsRunActive))
                    {
                        _step = CoachStep.Dodge;
                    }

                    break;
                case CoachStep.Dodge:
                    if (IsDodgeLearned())
                    {
                        _step = CoachStep.Collect;
                    }

                    break;
                case CoachStep.Collect:
                    if (episode != null && episode.BossEncounterReady)
                    {
                        _step = CoachStep.Exit;
                    }

                    break;
            }

            _wasHolding = isHolding;
        }

        /// <summary>
        /// Шаг закрывается, когда игрок пережил рывок без потери работоспособности
        /// или уже списал технику броском.
        /// </summary>
        private bool IsDodgeLearned()
        {
            var isDashing = false;
            for (var i = 0; i < chasers.Length; i++)
            {
                var chaser = chasers[i];
                if (chaser == null)
                {
                    continue;
                }

                if (chaser.IsWrecked)
                {
                    return true;
                }

                isDashing |= chaser.IsDashing;
            }

            if (isDashing)
            {
                if (!_wasDashing)
                {
                    _integrityAtDashStart = run != null ? run.Integrity : 0;
                    _wasDashing = true;
                }

                return false;
            }

            if (!_wasDashing)
            {
                return false;
            }

            _wasDashing = false;
            return run == null || run.Integrity >= _integrityAtDashStart;
        }

        private string ResolveMessage()
        {
            if (Time.time < _beatUntil && !string.IsNullOrEmpty(_beatMessage))
            {
                return _beatMessage;
            }

            // Опасность важнее любого шага, пока уклонение ещё не выучено.
            if (_step <= CoachStep.Dodge && IsAnyChaserThreatening())
            {
                return Loc.Get(LocalizationTables.Office, "coach.dodge", DodgeMessage);
            }

            return _step switch
            {
                CoachStep.Move => Loc.Get(LocalizationTables.Office, "coach.move", MoveMessage),
                CoachStep.Approach => Loc.Get(LocalizationTables.Office, "coach.approach", ApproachMessage),
                CoachStep.Throw => Loc.Get(LocalizationTables.Office, "coach.throw", ThrowMessage),
                CoachStep.Dodge => Loc.Get(LocalizationTables.Office, "coach.dodge", DodgeMessage),
                CoachStep.Collect => Loc.Get(LocalizationTables.Office, "coach.collect", CollectMessage),
                _ => Loc.Get(LocalizationTables.Office, "coach.exit", ExitMessage)
            };
        }

        private bool IsAnyChaserThreatening()
        {
            for (var i = 0; i < chasers.Length; i++)
            {
                var chaser = chasers[i];
                if (chaser != null && (chaser.IsTelegraphing || chaser.IsDashing))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
