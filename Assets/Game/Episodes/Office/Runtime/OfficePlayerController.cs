using UnityEngine;
using UnityEngine.InputSystem;

namespace Jam.Episodes.Office
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class OfficePlayerController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string aimActionName = "Aim";

        [Header("Movement")]
        [SerializeField] private Camera movementCamera;
        [SerializeField] private OfficeMomentum momentum;
        [SerializeField, Min(0.1f)] private float moveSpeed = 10.5f;
        [SerializeField, Min(0.1f)] private float acceleration = 46f;
        [SerializeField, Min(0.1f)] private float rotationSpeed = 720f;
        [SerializeField, Min(0f)] private float groundPressure = 2f;
        [SerializeField, Min(0f)] private float ramMomentumMultiplier = 0.4f;
        [SerializeField, Range(0f, 1f)] private float ramSpeedRetention = 0.9f;

        [Header("Aim")]
        [SerializeField, Range(0f, 1f)] private float directionalAimDeadzone = 0.2f;

        private CharacterController _characterController;
        private InputAction _moveAction;
        private InputAction _aimAction;
        private Vector3 _planarVelocity;
        private Vector3 _aimDirection;
        private Vector2 _pointerPosition;
        private Vector2 _directionalAim;
        private AimSource _aimSource;
        private bool _ownsMoveActionEnable;
        private bool _ownsAimActionEnable;
        private bool _controlLocked;

        private enum AimSource
        {
            None,
            Pointer,
            Direction
        }

        public bool IsControlLocked => _controlLocked;

        public float PlanarSpeed => _planarVelocity.magnitude;

        public Vector3 AimDirection => _aimDirection;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            movementCamera ??= Camera.main;
            ResetAimDirection();
        }

        private void OnEnable()
        {
            ResolveMoveAction();
        }

        private void OnDisable()
        {
            if (_aimAction != null)
            {
                _aimAction.performed -= OnAimPerformed;
            }

            if (_ownsMoveActionEnable && _moveAction != null)
            {
                _moveAction.Disable();
            }

            if (_ownsAimActionEnable && _aimAction != null)
            {
                _aimAction.Disable();
            }

            _ownsMoveActionEnable = false;
            _ownsAimActionEnable = false;
        }

        private void Update()
        {
            // Быстрый restart на кадр выключает CharacterController, чтобы перенести
            // героя; двигать его в этот момент нельзя.
            if (!_characterController.enabled)
            {
                return;
            }

            if (_controlLocked)
            {
                ResetMotion();
                return;
            }

            var input = _moveAction != null
                ? Vector2.ClampMagnitude(_moveAction.ReadValue<Vector2>(), 1f)
                : Vector2.zero;
            var desiredDirection = GetCameraRelativeDirection(input);
            var speed = momentum != null ? moveSpeed * momentum.SpeedMultiplier : moveSpeed;
            var desiredVelocity = desiredDirection * speed;

            _planarVelocity = Vector3.MoveTowards(
                _planarVelocity,
                desiredVelocity,
                acceleration * Time.deltaTime);

            // Простой роняет Momentum, поэтому шкала считает реальную скорость героя.
            momentum?.ReportPlanarSpeed(_planarVelocity.magnitude);

            var motion = _planarVelocity;
            motion.y = -groundPressure;
            _characterController.Move(motion * Time.deltaTime);

            UpdateAimDirection();
            if (_aimDirection.sqrMagnitude > 0.001f)
            {
                var targetRotation = Quaternion.LookRotation(_aimDirection, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime);
            }
        }

        public void Configure(
            InputActionAsset actions,
            Camera camera,
            string mapName,
            string moveName,
            string aimName,
            OfficeMomentum momentumScale = null)
        {
            inputActions = actions;
            movementCamera = camera;
            actionMapName = mapName;
            moveActionName = moveName;
            aimActionName = aimName;
            momentum = momentumScale;
        }

        /// <summary>Гасит инерцию героя; используется быстрым restart забега.</summary>
        public void ResetMotion()
        {
            _planarVelocity = Vector3.zero;
            momentum?.ReportPlanarSpeed(0f);
            ResetAimDirection();
        }

        /// <summary>Короткая постановка блокирует движение, не отключая input map.</summary>
        public void SetControlLocked(bool value)
        {
            _controlLocked = value;
            if (value)
            {
                ResetMotion();
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (_controlLocked || hit.collider == null)
            {
                return;
            }

            var breakable = hit.collider.GetComponentInParent<OfficeBreakable>();
            if (breakable == null)
            {
                return;
            }

            // Контракт веб-демо: реальная скорость героя усиливается текущим
            // Momentum. После успешного тарана часть скорости сохраняется.
            var momentumValue = momentum != null ? momentum.Value : 0f;
            var ramImpact = PlanarSpeed * (1f + (momentumValue * ramMomentumMultiplier));
            if (breakable.TryTakeImpact(ramImpact))
            {
                _planarVelocity *= ramSpeedRetention;
            }
        }

        private void ResolveMoveAction()
        {
            if (inputActions == null)
            {
                Debug.LogError($"{nameof(OfficePlayerController)} on '{name}' has no Input Actions asset assigned.", this);
                return;
            }

            var actionMap = inputActions.FindActionMap(actionMapName, false);
            if (actionMap == null)
            {
                Debug.LogError($"Input action map '{actionMapName}' was not found for the office player.", this);
                return;
            }

            _moveAction = actionMap.FindAction(moveActionName, false);
            if (_moveAction == null)
            {
                Debug.LogError($"Input action '{actionMapName}/{moveActionName}' was not found for the office player.", this);
            }
            else if (!_moveAction.enabled)
            {
                _moveAction.Enable();
                _ownsMoveActionEnable = true;
            }

            _aimAction = actionMap.FindAction(aimActionName, false);
            if (_aimAction == null)
            {
                Debug.LogError($"Input action '{actionMapName}/{aimActionName}' was not found for the office player.", this);
                return;
            }

            _aimAction.performed -= OnAimPerformed;
            _aimAction.performed += OnAimPerformed;
            if (!_aimAction.enabled)
            {
                _aimAction.Enable();
                _ownsAimActionEnable = true;
            }
        }

        private void OnAimPerformed(InputAction.CallbackContext context)
        {
            var value = context.ReadValue<Vector2>();
            if (IsDirectionalAimBinding(context.control))
            {
                if (value.sqrMagnitude < directionalAimDeadzone * directionalAimDeadzone)
                {
                    return;
                }

                _directionalAim = Vector2.ClampMagnitude(value, 1f);
                _aimSource = AimSource.Direction;
                return;
            }

            _pointerPosition = value;
            _aimSource = AimSource.Pointer;
        }

        private bool IsDirectionalAimBinding(InputControl control)
        {
            if (_aimAction == null || control == null)
            {
                return false;
            }

            var bindingIndex = _aimAction.GetBindingIndexForControl(control);
            if (bindingIndex < 0 || bindingIndex >= _aimAction.bindings.Count)
            {
                return false;
            }

            var groups = _aimAction.bindings[bindingIndex].groups;
            return !string.IsNullOrEmpty(groups) && groups.Contains("Gamepad");
        }

        private void UpdateAimDirection()
        {
            Vector3 direction;
            switch (_aimSource)
            {
                case AimSource.Pointer:
                    if (!TryGetPointerDirection(out direction))
                    {
                        return;
                    }

                    break;
                case AimSource.Direction:
                    direction = GetCameraRelativeDirection(_directionalAim);
                    break;
                default:
                    return;
            }

            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                _aimDirection = direction.normalized;
            }
        }

        private bool TryGetPointerDirection(out Vector3 direction)
        {
            direction = Vector3.zero;
            if (movementCamera == null)
            {
                return false;
            }

            var ray = movementCamera.ScreenPointToRay(_pointerPosition);
            var aimPlane = new Plane(Vector3.up, transform.position);
            if (!aimPlane.Raycast(ray, out var distance))
            {
                return false;
            }

            direction = ray.GetPoint(distance) - transform.position;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f;
        }

        private void ResetAimDirection()
        {
            _aimDirection = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (_aimDirection.sqrMagnitude <= 0.001f)
            {
                _aimDirection = Vector3.forward;
            }
        }

        private Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            if (movementCamera == null)
            {
                return new Vector3(input.x, 0f, input.y);
            }

            var cameraForward = Vector3.ProjectOnPlane(movementCamera.transform.forward, Vector3.up).normalized;
            var cameraRight = Vector3.ProjectOnPlane(movementCamera.transform.right, Vector3.up).normalized;
            var direction = (cameraRight * input.x) + (cameraForward * input.y);
            return Vector3.ClampMagnitude(direction, 1f);
        }
    }
}
