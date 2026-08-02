using UnityEngine;

namespace Jam.Episodes.Photo
{
    [DisallowMultipleComponent]
    public sealed class PhotoPortraitPose : MonoBehaviour
    {
        public enum PoseStyle
        {
            Heroine,
            Mother,
            Officer
        }

        [SerializeField] private PoseStyle style;

        private Animator _animator;
        private Transform _head;
        private Transform _leftUpperArm;
        private Transform _leftLowerArm;
        private Transform _leftHand;
        private Transform _rightUpperArm;
        private Transform _rightLowerArm;
        private Transform _rightHand;
        private Quaternion _headBindRotation;

        public void Configure(PoseStyle value)
        {
            style = value;
        }

        private void Awake()
        {
            CacheBones();
        }

        private void OnEnable()
        {
            CacheBones();
        }

        private void LateUpdate()
        {
            if (_animator == null || !_animator.isHuman) return;

            var forward = transform.forward;
            var side = transform.right;
            var heroine = style == PoseStyle.Heroine;
            var officer = style == PoseStyle.Officer;
            PoseArm(
                _leftUpperArm,
                _leftLowerArm,
                _leftHand,
                -Vector3.up + (heroine ? -0.16f : officer ? -0.04f : -0.08f) * side + 0.06f * forward);
            PoseArm(
                _rightUpperArm,
                _rightLowerArm,
                _rightHand,
                -Vector3.up + (heroine ? 0.16f : officer ? 0.04f : 0.08f) * side + 0.06f * forward);

            if (_head != null)
            {
                var tilt = heroine ? new Vector3(2f, -3f, -4f) : officer ? new Vector3(0f, 0f, 0f) : new Vector3(-1f, 4f, 3f);
                _head.localRotation = _headBindRotation * Quaternion.Euler(tilt);
            }
        }

        private void CacheBones()
        {
            if (_animator != null) return;
            _animator = GetComponentInChildren<Animator>(true);
            if (_animator == null || !_animator.isHuman) return;
            _head = _animator.GetBoneTransform(HumanBodyBones.Head);
            _leftUpperArm = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            _leftLowerArm = _animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            _leftHand = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
            _rightUpperArm = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            _rightLowerArm = _animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            _rightHand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (_head != null) _headBindRotation = _head.localRotation;
        }

        private static void PoseArm(Transform upper, Transform lower, Transform hand, Vector3 desiredDirection)
        {
            if (upper == null || lower == null) return;
            var currentUpper = lower.position - upper.position;
            if (currentUpper.sqrMagnitude > 0.0001f)
            {
                upper.rotation = Quaternion.FromToRotation(currentUpper, desiredDirection.normalized) * upper.rotation;
            }

            if (hand == null) return;
            var currentLower = hand.position - lower.position;
            if (currentLower.sqrMagnitude > 0.0001f)
            {
                lower.rotation = Quaternion.FromToRotation(currentLower, desiredDirection.normalized) * lower.rotation;
            }
        }
    }
}
