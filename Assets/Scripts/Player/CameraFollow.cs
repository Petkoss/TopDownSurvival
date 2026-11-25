using UnityEngine;

namespace Survival.Player
{
    /// <summary>Smoothly keeps the camera centred on a target.</summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] float smoothTime = 0.12f;
        [SerializeField] Vector2 offset = new Vector2(0f, 0.5f);

        Vector3 _velocity;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);

            // Snap on large jumps (spawn / load) instead of sliding across the map.
            if ((desired - transform.position).sqrMagnitude > 100f)
            {
                transform.position = desired;
                _velocity = Vector3.zero;
                return;
            }
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
        }
    }
}
