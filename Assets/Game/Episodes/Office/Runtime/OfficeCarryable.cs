using System.Collections.Generic;
using UnityEngine;

namespace Jam.Episodes.Office
{
    /// <summary>
    /// Лёгкий переносимый предмет: автоматически выбирается свободными руками,
    /// бросается по <c>Primary</c> и на лету разрушает подготовленные объекты.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class OfficeCarryable : MonoBehaviour
    {
        private static readonly List<OfficeCarryable> ActiveItems = new();

        [SerializeField] private string displayName = "КЛАВИАТУРА";
        [SerializeField] private Renderer highlightRenderer;
        [SerializeField] private Collider bodyCollider;
        [SerializeField, Min(0f)] private float thrownDuration = 2.5f;
        [SerializeField] private float fallResetHeight = -6f;

        private Rigidbody _rigidbody;
        private Transform _releaseParent;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private float _pickupUnlockTime;
        private float _thrownUntil;

        /// <summary>Все предметы сцены; заменяет физический запрос при выборе цели.</summary>
        public static IReadOnlyList<OfficeCarryable> Active => ActiveItems;

        public string DisplayName => displayName;

        public bool IsHeld { get; private set; }

        public bool IsAvailable => !IsHeld && Time.time >= _pickupUnlockTime;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _releaseParent = transform.parent;
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;

            if (bodyCollider == null)
            {
                bodyCollider = GetComponent<Collider>();
            }
        }

        private void Update()
        {
            // Бросок может вынести предмет за пределы этажа: возвращаем его на место,
            // чтобы петля «подбор → бросок» не осталась без единственного предмета.
            if (!IsHeld && transform.position.y < fallResetHeight)
            {
                ResetToSpawn();
            }
        }

        private void OnEnable()
        {
            ActiveItems.Add(this);
            SetHighlighted(false);
        }

        private void OnDisable()
        {
            ActiveItems.Remove(this);
        }

        public void SetHighlighted(bool value)
        {
            if (highlightRenderer != null)
            {
                highlightRenderer.enabled = value && IsAvailable;
            }
        }

        public void Attach(Transform hand)
        {
            IsHeld = true;
            SetHighlighted(false);

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;

            if (bodyCollider != null)
            {
                bodyCollider.enabled = false;
            }

            transform.SetParent(hand, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        public void Throw(Vector3 direction, float force, float pickupLockout)
        {
            IsHeld = false;
            transform.SetParent(_releaseParent, true);

            if (bodyCollider != null)
            {
                bodyCollider.enabled = true;
            }

            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = direction * force;
            _rigidbody.angularVelocity = new Vector3(7f, 2.5f, 4.5f);

            _pickupUnlockTime = Time.time + pickupLockout;
            _thrownUntil = Time.time + thrownDuration;
        }

        public void ResetToSpawn()
        {
            transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _pickupUnlockTime = 0f;
            _thrownUntil = 0f;
        }

        public void Configure(string itemName, Renderer highlight, Collider body)
        {
            displayName = itemName;
            highlightRenderer = highlight;
            bodyCollider = body;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsHeld || Time.time > _thrownUntil)
            {
                return;
            }

            var breakable = collision.collider.GetComponentInParent<OfficeBreakable>();
            if (breakable == null)
            {
                return;
            }

            breakable.TryBreak(collision.relativeVelocity.magnitude);
        }
    }
}
