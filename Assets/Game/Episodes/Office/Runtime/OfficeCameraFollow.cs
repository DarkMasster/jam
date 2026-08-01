using UnityEngine;

namespace Jam.Episodes.Office
{
    [RequireComponent(typeof(Camera))]
    public sealed class OfficeCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(0f, 18f, -14f);
        [SerializeField] private Vector3 lookOffset = new(0f, 0f, 3f);
        [SerializeField, Min(0.1f)] private float followSharpness = 6.5f;

        private void Start()
        {
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var desiredPosition = target.position + offset;
            var blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, blend);
            transform.LookAt(target.position + lookOffset, Vector3.up);
        }

        public void Configure(Transform followTarget, Vector3 cameraOffset, Vector3 cameraLookOffset)
        {
            target = followTarget;
            offset = cameraOffset;
            lookOffset = cameraLookOffset;
        }

        private void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            transform.position = target.position + offset;
            transform.LookAt(target.position + lookOffset, Vector3.up);
        }
    }
}
