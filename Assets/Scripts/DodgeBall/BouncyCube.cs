using UnityEngine;

namespace DodgeBall
{
    // 평범한 큐브처럼 보이다가, 플레이어가 닿으면 여섯 면(panels)이 깜짝상자처럼
    // 띠용 튀어나왔다 들어가며 플레이어를 밀어낸다. 바닥 밖으로 밀려나는 것은 PlayerController가 막는다.
    public class BouncyCube : MonoBehaviour
    {
        public Transform[] panels;
        public float popDistance = 0.45f;
        [Tooltip("튀어나온 면이 출렁이는 속도")]
        public float wobbleSpeed = 18f;
        [Tooltip("출렁임이 잦아드는 속도")]
        public float settleSpeed = 4f;
        public float pushSpeed = 7f;
        public float cooldown = 1f;

        Vector3[] restPositions;
        float popTime = float.NegativeInfinity;

        void Awake()
        {
            restPositions = new Vector3[panels.Length];
            for (int i = 0; i < panels.Length; i++) restPositions[i] = panels[i].localPosition;
        }

        void OnCollisionEnter(Collision collision)
        {
            PlayerController player = collision.collider.GetComponent<PlayerController>();
            if (player == null || Time.time - popTime < cooldown) return;

            popTime = Time.time;
            Vector3 direction = player.transform.position - transform.position;
            direction.y = 0f;
            player.Knockback(direction.normalized * pushSpeed);
        }

        void Update()
        {
            // 감쇠 진동: 확 튀어나왔다가 안팎으로 몇 번 출렁이고 제자리로
            float t = Time.time - popTime;
            float offset = t < 1.5f ? popDistance * Mathf.Exp(-settleSpeed * t) * Mathf.Sin(t * wobbleSpeed) : 0f;
            for (int i = 0; i < panels.Length; i++)
            {
                panels[i].localPosition = restPositions[i] + restPositions[i].normalized * offset;
            }
        }
    }
}
