using UnityEngine;

namespace Dracolly
{
    /// <summary>Kamerayı karaktere yumuşakça takip ettirir.</summary>
    public class CameraFollow2D : MonoBehaviour
    {
        public Transform target;
        public Vector2 offset = new Vector2(1.2f, 1.3f);
        public float smoothTime = 0.18f;

        Vector3 velocity;

        void LateUpdate()
        {
            if (target == null) return;
            // karakter hangi yöne bakıyorsa kamera biraz o tarafa kayar
            float facing = 1f;
            var pc = target.GetComponent<PlayerController2D>();
            if (pc != null && pc.visual != null) facing = Mathf.Sign(pc.visual.localScale.x);

            Vector3 goal = new Vector3(target.position.x + offset.x * facing, target.position.y + offset.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
        }
    }
}
