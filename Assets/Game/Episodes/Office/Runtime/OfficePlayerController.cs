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

        [Header("Movement")]
        [SerializeField] private Camera movementCamera;
        [SerializeField] private OfficeMomentum momentum;
        [SerializeField, Min(0.1f)] private float moveSpeed = 10.5f;
        [SerializeField, Min(0.1f)] private float acceleration = 46f;
        [SerializeField, Min(0.1f)] private float rotationSpeed = 720f;
        [SerializeField, Min(0f)] private float groundPressure = 2f;
        [SerializeField, Min(0f)] private float ramMomentumMultiplier = 0.4f;
        [SerializeField, Range(0f, 1f)] private float ramSpeedRetention = 0.9f;

        private CharacterController _characterController;
        private InputAction _moveAction;
        private Vector3 _planarVelocity;
        private bool _ownsMoveActionEnable;
        private bool _controlLocked;

        public bool IsControlLocked => _controlLocked;

        public float PlanarSpeed => _planarVelocity.magnitude;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            movementCamera ??= Camera.main;
        }

        private void OnEnable()
        {
            ResolveMoveAction();
        }

        private void OnDisable()
        {
            if (_ownsMoveActionEnable && _moveAction != null)
            {
                _moveAction.Disable();
            }

            _ownsMoveActionEnable = false;
        }

        private void Update()
        {
            // Быстрый restart на кадр выключает CharacterController, чтобы перенести
            // героя; двигать его в этот момент нельзя.
            if (_moveAction == null || !_characterController.enabled)
            {
                return;
            }

            if (_controlLocked)
            {
                ResetMotion();
                return;
            }

            var input = Vector2.ClampMagnitude(_moveAction.ReadValue<Vector2>(), 1f);
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

            if (desiredDirection.sqrMagnitude > 0.001f)
            {
                var targetRotation = Quaternion.LookRotation(desiredDirection, Vector3.up);
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
            string actionName,
            OfficeMomentum momentumScale = null)
        {
            inputActions = actions;
            movementCamera = camera;
            actionMapName = mapName;
            moveActionName = actionName;
            momentum = momentumScale;
        }

        /// <summary>Гасит инерцию героя; используется быстрым restart забега.</summary>
        public void ResetMotion()
        {
            _planarVelocity = Vector3.zero;
            momentum?.ReportPlanarSpeed(0f);
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
                return;
            }

            if (!_moveAction.enabled)
            {
                _moveAction.Enable();
                _ownsMoveActionEnable = true;
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
