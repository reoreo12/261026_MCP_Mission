using System.Collections;
using UnityEngine;

namespace DodgeBall
{
    // 떨어지는 공. 중력을 더 세게 받아 묵직하게 떨어지고, 처음 닿는 순간
    // 화면 흔들림 + 먼지 + 찌그러짐 연출을 하고 착지 표시를 지운다.
    [RequireComponent(typeof(Rigidbody))]
    public class FallingBall : MonoBehaviour
    {
        [Tooltip("1보다 크면 기본 중력보다 빨리 떨어진다")]
        public float gravityScale = 1.6f;
        public float shakeAmount = 0.6f;
        public float squashTime = 0.18f;

        Rigidbody body;
        LandingIndicator indicator;
        float startHeight;
        float landingHeight;
        bool landed;

        public void Init(LandingIndicator landingIndicator, float groundHeight)
        {
            indicator = landingIndicator;
            landingHeight = groundHeight;
        }

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            startHeight = transform.position.y;
        }

        void FixedUpdate()
        {
            if (!landed) body.AddForce(Physics.gravity * (gravityScale - 1f), ForceMode.Acceleration);
        }

        void Update()
        {
            if (!landed && indicator != null)
            {
                // 공 아랫면이 착지점에 가까워질수록 1
                indicator.SetProgress(Mathf.InverseLerp(startHeight, landingHeight + 0.5f, transform.position.y));
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (landed) return;
            landed = true;

            float strength = Mathf.Clamp01(collision.relativeVelocity.magnitude / 15f);
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(shakeAmount * strength);
            if (DustEffect.Instance != null) DustEffect.Instance.Play(collision.GetContact(0).point, strength);
            RemoveIndicator();
            StartCoroutine(Squash());
        }

        void OnDestroy()
        {
            if (gameObject.scene.isLoaded) RemoveIndicator();
        }

        void RemoveIndicator()
        {
            if (indicator != null) Destroy(indicator.gameObject);
            indicator = null;
        }

        // 착지 순간 납작해졌다가 돌아와 무게감을 준다
        IEnumerator Squash()
        {
            Vector3 baseScale = transform.localScale;
            Vector3 squashed = Vector3.Scale(baseScale, new Vector3(1.25f, 0.7f, 1.25f));
            for (float t = 0f; t < squashTime; t += Time.deltaTime)
            {
                transform.localScale = Vector3.Lerp(squashed, baseScale, t / squashTime);
                yield return null;
            }
            transform.localScale = baseScale;
        }
    }
}
