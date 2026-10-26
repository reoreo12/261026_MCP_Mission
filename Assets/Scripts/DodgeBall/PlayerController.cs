using UnityEngine;
using UnityEngine.InputSystem;

namespace DodgeBall
{
    // WASD로 바닥 위를 움직이는 캡슐. Obstacle 태그(낙하 공)에 닿으면 Game Over 후 멈춘다.
    // 전기 큐브에 닿으면 스턴, 튕기는 큐브에 닿으면 밀려난다(바닥 밖으로는 나가지 않음).
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        public float speed = 5f;
        [Tooltip("바닥 밖으로 나가지 않도록 x, z를 이 값 안으로 제한")]
        public float boundary = 4.5f;

        [Header("Stun")]
        [Tooltip("스턴 중 켜지는 전기 이펙트")]
        public GameObject stunEffect;
        [Tooltip("스턴이 끝난 뒤 다시 스턴되지 않는 시간")]
        public float stunImmunity = 1f;
        public Color stunFlashColor = new Color(1f, 0.95f, 0.3f);
        public float stunKnockback = 3f;

        [Header("Knockback")]
        [Tooltip("밀려나는 속도가 초당 줄어드는 양")]
        public float knockbackDecay = 12f;

        public bool IsStunned => Time.time < stunnedUntil;

        Rigidbody body;
        Renderer bodyRenderer;
        MaterialPropertyBlock block;
        Vector3 input;
        Vector3 knockback;
        bool stopped;
        float stunnedUntil = float.NegativeInfinity;
        float immuneUntil = float.NegativeInfinity;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            bodyRenderer = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
            if (stunEffect != null) stunEffect.SetActive(false);
        }

        // 입력은 Update에서 읽고, 물리 이동은 FixedUpdate에서 적용
        void Update()
        {
            UpdateStunVisual();

            input = Vector3.zero;
            Keyboard keyboard = Keyboard.current;
            if (stopped || IsStunned || keyboard == null) return;

            if (keyboard.wKey.isPressed) input.z += 1f;
            if (keyboard.sKey.isPressed) input.z -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            input = Vector3.ClampMagnitude(input, 1f);
        }

        void FixedUpdate()
        {
            if (stopped) return;

            Vector3 velocity = input * speed + knockback;
            velocity.y = body.linearVelocity.y;
            body.linearVelocity = velocity;
            knockback = Vector3.MoveTowards(knockback, Vector3.zero, knockbackDecay * Time.fixedDeltaTime);

            // 밀려나더라도 바닥 밖으로는 나가지 않게 위치를 제한한다
            Vector3 position = body.position;
            Vector3 clamped = new Vector3(Mathf.Clamp(position.x, -boundary, boundary), position.y, Mathf.Clamp(position.z, -boundary, boundary));
            if (clamped != position) body.position = clamped;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (stopped || !collision.gameObject.CompareTag("Obstacle")) return;
            Stop();
            GameManager.Instance.GameOver();
        }

        public void Stun(float duration, Vector3 source)
        {
            if (stopped || Time.time < immuneUntil) return;
            stunnedUntil = Time.time + duration;
            immuneUntil = stunnedUntil + stunImmunity;

            // 큐브에서 살짝 튕겨 나가 계속 붙어 있지 않게 한다
            Vector3 away = transform.position - source;
            away.y = 0f;
            Knockback(away.normalized * stunKnockback);
        }

        public void Knockback(Vector3 velocity)
        {
            if (stopped) return;
            velocity.y = 0f;
            knockback = velocity;
        }

        public void Stop()
        {
            stopped = true;
            stunnedUntil = float.NegativeInfinity;
            body.linearVelocity = Vector3.zero;
            body.isKinematic = true;
        }

        // 스턴 중에는 전기 이펙트를 켜고 몸이 노랗게 깜빡인다
        void UpdateStunVisual()
        {
            bool stunned = IsStunned;
            if (stunEffect != null && stunEffect.activeSelf != stunned) stunEffect.SetActive(stunned);

            if (stunned && Mathf.Repeat(Time.time * 12f, 1f) < 0.5f) block.SetColor("_BaseColor", stunFlashColor);
            else block.Clear();
            bodyRenderer.SetPropertyBlock(block);
        }
    }
}
