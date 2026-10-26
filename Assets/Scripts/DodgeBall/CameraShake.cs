using UnityEngine;

namespace DodgeBall
{
    // Main Camera에 붙어 흔들림을 만든다. Shake()로 쌓인 흔들림(trauma)이 시간에 따라 줄어든다.
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        public float maxOffset = 0.35f;
        public float maxAngle = 1.5f;
        public float frequency = 25f;
        [Tooltip("초당 줄어드는 흔들림 양")]
        public float decay = 2.5f;

        Vector3 basePosition;
        Quaternion baseRotation;
        float trauma;

        void Awake()
        {
            Instance = this;
            basePosition = transform.localPosition;
            baseRotation = transform.localRotation;
        }

        public void Shake(float amount)
        {
            trauma = Mathf.Clamp01(trauma + amount);
        }

        void LateUpdate()
        {
            if (trauma <= 0f)
            {
                transform.SetLocalPositionAndRotation(basePosition, baseRotation);
                return;
            }

            // 제곱해서 약한 흔들림은 더 약하게, 강한 흔들림은 확실하게 느껴지도록 한다
            float power = trauma * trauma;
            float t = Time.time * frequency;
            Vector3 offset = new Vector3(Noise(t, 0f), Noise(0f, t), Noise(t, t)) * (maxOffset * power);
            Quaternion tilt = Quaternion.Euler(0f, 0f, Noise(t, 7f) * maxAngle * power);
            transform.SetLocalPositionAndRotation(basePosition + offset, baseRotation * tilt);

            trauma = Mathf.Max(0f, trauma - decay * Time.deltaTime);
        }

        static float Noise(float x, float y)
        {
            return Mathf.PerlinNoise(x, y) * 2f - 1f;
        }
    }
}
