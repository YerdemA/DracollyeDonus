using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Dracolly
{
    /// <summary>
    /// Basit platformer hareketi: sağ-sol yürüme ve zıplama.
    /// Şimdilik klavye (A/D veya oklar, Boşluk/W ile zıplama) ve ekrana dokunma ile çalışır.
    /// İleride sanal joystick SetMoveInput / PressJump metotlarını çağıracak.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        [Header("Hareket")]
        public float moveSpeed = 3.2f;
        public float acceleration = 30f;
        public float jumpVelocity = 8.5f;
        [Tooltip("Kenardan düştükten sonra kısa süre daha zıplanabilir (sn).")]
        public float coyoteTime = 0.1f;
        [Tooltip("Yere değmeden hemen önce basılan zıplama hatırlanır (sn).")]
        public float jumpBuffer = 0.12f;

        [Header("Bağlantılar")]
        public Transform visual;
        public CutoutWalk walk;

        [Header("Mobil test")]
        [Tooltip("Ekranın sol/sağ yarısına basılı tutunca yürür, iki parmakla zıplar.")]
        public bool simpleTouchControls = true;

        Rigidbody2D rb;
        ContactFilter2D groundFilter;
        float inputX;
        float externalX;
        bool hasExternalInput;
        float lastGroundedTime = -10f;
        float lastJumpPressTime = -10f;
        bool grounded;

        public bool IsGrounded => grounded;

        /// <summary>Sanal joystick buradan yön verir (-1..1).</summary>
        public void SetMoveInput(float x)
        {
            externalX = Mathf.Clamp(x, -1f, 1f);
            hasExternalInput = true;
        }

        /// <summary>Zıplama butonu buradan çağırır.</summary>
        public void PressJump() => lastJumpPressTime = Time.time;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            groundFilter = new ContactFilter2D
            {
                useNormalAngle = true,
                minNormalAngle = 45f,
                maxNormalAngle = 135f,
                useTriggers = false
            };
        }

        void Update()
        {
            float x = 0f;
            bool jumpPressed = false;
            bool touchHandled = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
                if (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
                    jumpPressed = true;
            }
            if (simpleTouchControls && Touchscreen.current != null)
            {
                int down = 0;
                foreach (var t in Touchscreen.current.touches)
                {
                    if (!t.press.isPressed) continue;
                    down++;
                    x += t.position.ReadValue().x < Screen.width * 0.5f ? -1f : 1f;
                    if (t.press.wasPressedThisFrame && down >= 2) jumpPressed = true;
                }
                touchHandled = down > 0;
            }
#else
            x = Input.GetAxisRaw("Horizontal");
            if (Input.GetButtonDown("Jump")) jumpPressed = true;
            if (simpleTouchControls && Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    x += t.position.x < Screen.width * 0.5f ? -1f : 1f;
                }
                if (Input.touchCount >= 2 && Input.GetTouch(1).phase == TouchPhase.Began) jumpPressed = true;
                touchHandled = true;
            }
#endif
            if (touchHandled) x = Mathf.Clamp(x, -1f, 1f);
            if (hasExternalInput && Mathf.Abs(externalX) > 0.01f) x = externalX;
            inputX = Mathf.Clamp(x, -1f, 1f);
            if (jumpPressed) lastJumpPressTime = Time.time;

            // karakteri gittiği yöne çevir
            if (visual != null && Mathf.Abs(inputX) > 0.01f)
                visual.localScale = new Vector3(Mathf.Sign(inputX), 1f, 1f);
        }

        void FixedUpdate()
        {
            grounded = rb.IsTouching(groundFilter);
            if (grounded) lastGroundedTime = Time.time;
            if (walk != null) walk.grounded = grounded;

            Vector2 v = GetVelocity();
            v.x = Mathf.MoveTowards(v.x, inputX * moveSpeed, acceleration * Time.fixedDeltaTime);

            bool canJump = Time.time - lastGroundedTime <= coyoteTime;
            bool wantsJump = Time.time - lastJumpPressTime <= jumpBuffer;
            if (canJump && wantsJump)
            {
                v.y = jumpVelocity;
                lastJumpPressTime = -10f;
                lastGroundedTime = -10f;
            }
            SetVelocity(v);
        }

        Vector2 GetVelocity()
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        void SetVelocity(Vector2 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }
    }
}
