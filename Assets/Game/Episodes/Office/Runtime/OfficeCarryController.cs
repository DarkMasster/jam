using UnityEngine;
using UnityEngine.InputSystem;

namespace Jam.Episodes.Office
{
    /// <summary>
    /// Автоматический подбор и бросок офисных предметов. Отдельной кнопки подбора нет:
    /// свободные руки берут ближайшую цель в небольшой зоне перед героем, а занятые
    /// руки не заменяют предмет новым.
    /// </summary>
    public sealed class OfficeCarryController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string primaryActionName = "Attack";

        [Header("Scene")]
        [SerializeField] private Transform handAnchor;
        [SerializeField] private OfficeEpisodeController episodeController;
        [SerializeField] private OfficeMomentum momentum;

        [Header("Автоматический подбор")]
        [SerializeField, Min(0.1f)] private float scanForwardOffset = 0.9f;
        [SerializeField, Min(0.1f)] private float scanRadius = 3.4f;
        [SerializeField, Range(-1f, 1f)] private float minForwardDot = 0.1f;
        [SerializeField, Min(0.1f)] private float pickupRadius = 1.4f;

        [Header("Бросок")]
        [SerializeField, Min(1f)] private float throwForce = 17f;
        [SerializeField, Range(0f, 1f)] private float throwLift = 0.18f;
        [SerializeField, Min(0f)] private float pickupLockout = 0.8f;

        private InputAction _primaryAction;
        private OfficePlayerController _playerController;
        private bool _ownsPrimaryActionEnable;
        private OfficeCarryable _heldItem;
        private OfficeCarryable _highlightedItem;
        private bool _controlLocked;

        public OfficeCarryable HeldItem => _heldItem;

        public bool IsControlLocked => _controlLocked;

        private void Awake()
        {
            _playerController = GetComponent<OfficePlayerController>();
        }

        private void OnEnable()
        {
            ResolvePrimaryAction();
        }

        private void OnDisable()
        {
            if (_ownsPrimaryActionEnable && _primaryAction != null)
            {
                _primaryAction.Disable();
            }

            _ownsPrimaryActionEnable = false;
            Highlight(null);
            ReleaseHeldItem();
        }

        private void Update()
        {
            if (_controlLocked)
            {
                Highlight(null);
                return;
            }

            if (_heldItem != null)
            {
                Highlight(null);

                if (_primaryAction != null && _primaryAction.WasPressedThisFrame())
                {
                    ThrowHeldItem();
                }

                return;
            }

            var candidate = FindCandidate(out var distance);
            Highlight(candidate);

            if (candidate != null && distance <= pickupRadius)
            {
                PickUp(candidate);
            }
        }

        public void Configure(
            InputActionAsset actions,
            string mapName,
            string actionName,
            Transform hand,
            OfficeEpisodeController controller,
            OfficeMomentum momentumScale = null)
        {
            inputActions = actions;
            actionMapName = mapName;
            primaryActionName = actionName;
            handAnchor = hand;
            episodeController = controller;
            momentum = momentumScale;
        }

        /// <summary>
        /// Роняет предмет без броска. Быстрый restart использует это, чтобы предмет
        /// вернулся на своё место вместе с остальным забегом.
        /// </summary>
        public void ReleaseHeldItem()
        {
            if (_heldItem == null)
            {
                return;
            }

            var item = _heldItem;
            _heldItem = null;
            item.Release();
        }

        /// <summary>Постановка блокирует подбор и бросок, не роняя предмет из рук.</summary>
        public void SetControlLocked(bool value)
        {
            _controlLocked = value;
            if (value)
            {
                Highlight(null);
            }
        }

        private void PickUp(OfficeCarryable item)
        {
            if (handAnchor == null)
            {
                Debug.LogError($"{nameof(OfficeCarryController)} on '{name}' has no hand anchor assigned.", this);
                return;
            }

            _heldItem = item;
            item.Attach(handAnchor);
            episodeController?.ReportCarryPickup(item.DisplayName);
            OfficeFeedback.Instance?.ReportPickup(transform, item.DisplayName);
        }

        private void ThrowHeldItem()
        {
            var item = _heldItem;
            _heldItem = null;

            var direction = (GetAimDirection() + (Vector3.up * throwLift)).normalized;
            var force = momentum != null ? throwForce * momentum.ThrowMultiplier : throwForce;
            item.Throw(direction, force, pickupLockout);
            episodeController?.ReportCarryThrow(item.DisplayName);
            OfficeFeedback.Instance?.ReportThrow(transform.position);
        }

        private OfficeCarryable FindCandidate(out float bestDistance)
        {
            var forward = GetAimDirection();
            var origin = transform.position + (forward * scanForwardOffset);

            OfficeCarryable best = null;
            bestDistance = float.MaxValue;

            var items = OfficeCarryable.Active;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null || !item.IsAvailable)
                {
                    continue;
                }

                var offset = item.transform.position - origin;
                offset.y = 0f;

                var distance = offset.magnitude;
                if (distance > scanRadius || distance >= bestDistance)
                {
                    continue;
                }

                // Предмет прямо под руками считается выбранным независимо от направления.
                if (distance > pickupRadius && Vector3.Dot(forward, offset.normalized) < minForwardDot)
                {
                    continue;
                }

                best = item;
                bestDistance = distance;
            }

            return best;
        }

        private Vector3 GetAimDirection()
        {
            var direction = _playerController != null
                ? _playerController.AimDirection
                : transform.forward;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
        }

        private void Highlight(OfficeCarryable item)
        {
            if (_highlightedItem == item)
            {
                return;
            }

            if (_highlightedItem != null)
            {
                _highlightedItem.SetHighlighted(false);
            }

            _highlightedItem = item;

            if (_highlightedItem != null)
            {
                _highlightedItem.SetHighlighted(true);
            }
        }

        private void ResolvePrimaryAction()
        {
            if (inputActions == null)
            {
                Debug.LogError($"{nameof(OfficeCarryController)} on '{name}' has no Input Actions asset assigned.", this);
                return;
            }

            var actionMap = inputActions.FindActionMap(actionMapName, false);
            if (actionMap == null)
            {
                Debug.LogError($"Input action map '{actionMapName}' was not found for the office carry controller.", this);
                return;
            }

            _primaryAction = actionMap.FindAction(primaryActionName, false);
            if (_primaryAction == null)
            {
                Debug.LogError($"Input action '{actionMapName}/{primaryActionName}' was not found for the office carry controller.", this);
                return;
            }

            if (!_primaryAction.enabled)
            {
                _primaryAction.Enable();
                _ownsPrimaryActionEnable = true;
            }
        }
    }
}
