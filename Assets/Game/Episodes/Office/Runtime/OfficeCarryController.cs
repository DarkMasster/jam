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
        private bool _ownsPrimaryActionEnable;
        private OfficeCarryable _heldItem;
        private OfficeCarryable _highlightedItem;

        public OfficeCarryable HeldItem => _heldItem;

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
        }

        private void Update()
        {
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
            OfficeEpisodeController controller)
        {
            inputActions = actions;
            actionMapName = mapName;
            primaryActionName = actionName;
            handAnchor = hand;
            episodeController = controller;
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
        }

        private void ThrowHeldItem()
        {
            var item = _heldItem;
            _heldItem = null;

            var direction = (transform.forward + (Vector3.up * throwLift)).normalized;
            item.Throw(direction, throwForce, pickupLockout);
            episodeController?.ReportCarryThrow(item.DisplayName);
        }

        private OfficeCarryable FindCandidate(out float bestDistance)
        {
            var origin = transform.position + (transform.forward * scanForwardOffset);
            var forward = transform.forward;

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
