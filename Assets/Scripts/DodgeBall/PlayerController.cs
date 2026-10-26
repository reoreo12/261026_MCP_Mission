using UnityEngine;
using UnityEngine.InputSystem;

namespace DodgeBall
{
    // WASD로 바닥 위를 움직이는 캡슐. Obstacle 태그(낙하 공)에 닿으면 Game Over 후 멈춘다.
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        public float speed = 5f;
        [Tooltip("바닥 밖으로 나가지 않도록 x, z를 이 값 안으로 제한")]
        public float boundary = 4.5f;

        Rigidbody body;
        Vector3 input;
        bool stopped;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        // 입력은 Update에서 읽고, 물리 이동은 FixedUpdate에서 적용
        void Update()
        {
            input = Vector3.zero;
            Keyboard keyboard = Keyboard.current;
            if (stopped || keyboard == null) return;

            if (keyboard.wKey.isPressed) input.z += 1f;
            if (keyboard.sKey.isPressed) input.z -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            input = Vector3.ClampMagnitude(input, 1f);
        }

        void FixedUpdate()
        {
            if (stopped) return;

            Vector3 velocity = input * speed;
            velocity.y = body.linearVelocity.y;
            body.linearVelocity = velocity;

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

        public void Stop()
        {
            stopped = true;
            body.linearVelocity = Vector3.zero;
            body.isKinematic = true;
        }
    }
}
