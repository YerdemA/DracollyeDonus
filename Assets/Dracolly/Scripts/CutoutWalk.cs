using UnityEngine;

namespace Dracolly
{
    /// <summary>
    /// Kesme (cut-out) karakter için prosedürel yürüme / bekleme / zıplama pozu.
    /// Animasyon klibi gerektirmez: eklemleri her karede hıza göre döndürür.
    /// Açılar önizleme GIF'indekiyle aynıdır (pozitif açı = uzvu geriye atar).
    /// </summary>
    public class CutoutWalk : MonoBehaviour
    {
        [Header("Kemikler (kurulum menüsü doldurur)")]
        public Transform hip;
        public Transform torso, head;
        public Transform upperArm, forearm, hand, thigh, shin;
        public Transform upperArmBack, forearmBack, handBack, thighBack, shinBack;

        [Header("Hareket kaynağı")]
        public Rigidbody2D body;
        [Tooltip("PlayerController2D yere basıp basmadığını buraya yazar.")]
        public bool grounded = true;

        [Header("Yürüme ayarları")]
        [Tooltip("Bu hızda yürüme hareketi tam genliğe ulaşır (birim/sn).")]
        public float fullCycleSpeed = 3.2f;
        [Tooltip("Kat edilen her birimde kaç adım döngüsü atılır.")]
        public float cyclesPerUnit = 0.55f;
        public float thighSwing = 21f;
        public float kneeBend = 34f;
        public float armSwing = 20f;
        public float lean = 4f;
        [Tooltip("Adımlarda gövdenin aşağı inişi (birim).")]
        public float bob = 0.012f;

        float phase;
        float walkAmount;
        float airAmount;
        Vector3 hipRest;

        void Awake()
        {
            if (hip != null) hipRest = hip.localPosition;
            if (body == null) body = GetComponentInParent<Rigidbody2D>();
        }

        Vector2 Velocity()
        {
            if (body == null) return Vector2.zero;
#if UNITY_6000_0_OR_NEWER
            return body.linearVelocity;
#else
            return body.velocity;
#endif
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            float speed = Mathf.Abs(Velocity().x);

            walkAmount = Mathf.MoveTowards(walkAmount, Mathf.Clamp01(speed / fullCycleSpeed), dt * 6f);
            airAmount = Mathf.MoveTowards(airAmount, grounded ? 0f : 1f, dt * 8f);
            phase = Mathf.Repeat(phase + speed * cyclesPerUnit * dt, 1f);

            float w = phase * Mathf.PI * 2f;
            float s = Mathf.Sin(w), c = Mathf.Cos(w);
            float k = walkAmount;

            // bekleme: hafif nefes
            float breath = Mathf.Sin(Time.time * 2.1f) * (1f - k);

            // yürüme açıları (önizleme ile aynı)
            float tLean = lean * k + breath * 0.6f;
            float tThigh = -thighSwing * s * k;
            float tThighB = thighSwing * s * k;
            float tShin = (4f + kneeBend * Mathf.Max(0f, c)) * k;
            float tShinB = (4f + kneeBend * Mathf.Max(0f, -c)) * k;
            float tArm = armSwing * s * k - tLean + breath * 1.2f;
            float tArmB = -armSwing * s * k - tLean + breath * 1.2f;
            float tFore = (-12f - 10f * Mathf.Max(0f, -s)) * k - 3f * (1f - k);
            float tForeB = (-12f - 10f * Mathf.Max(0f, s)) * k - 3f * (1f - k);
            float tHead = -tLean + 1.5f * c * k;

            // havada: bacaklar toplanır, kollar açılır
            float a = airAmount;
            tThigh = Mathf.Lerp(tThigh, -28f, a);
            tShin = Mathf.Lerp(tShin, 42f, a);
            tThighB = Mathf.Lerp(tThighB, -8f, a);
            tShinB = Mathf.Lerp(tShinB, 30f, a);
            tArm = Mathf.Lerp(tArm, -35f, a);
            tArmB = Mathf.Lerp(tArmB, 25f, a);
            tFore = Mathf.Lerp(tFore, -25f, a);
            tForeB = Mathf.Lerp(tForeB, -20f, a);

            Rot(torso, tLean);
            Rot(head, tHead);
            Rot(upperArm, tArm);
            Rot(forearm, tFore);
            Rot(hand, -4f * k);
            Rot(upperArmBack, tArmB);
            Rot(forearmBack, tForeB);
            Rot(handBack, -4f * k);
            Rot(thigh, tThigh);
            Rot(shin, tShin);
            Rot(thighBack, tThighB);
            Rot(shinBack, tShinB);

            if (hip != null)
                hip.localPosition = hipRest + Vector3.down * (bob * Mathf.Abs(s) * k * (1f - a));
        }

        // Önizlemede pozitif açı saat yönündedir; Unity'de z ekseni saat yönünün tersidir.
        static void Rot(Transform t, float previewAngle)
        {
            if (t != null) t.localRotation = Quaternion.Euler(0f, 0f, -previewAngle);
        }
    }
}
