using UnityEngine;

namespace DodgeBall
{
    // 전기가 흐르는 큐브. 플레이어가 닿으면 1.5~2초 스턴. 발광 색이 맥박처럼 깜빡인다.
    public class ElectricCube : MonoBehaviour
    {
        public float minStun = 1.5f;
        public float maxStun = 2f;
        public Color glowColor = new Color(0.3f, 0.8f, 1f);
        public float glowSpeed = 8f;

        Renderer cubeRenderer;
        MaterialPropertyBlock block;

        void Awake()
        {
            cubeRenderer = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
        }

        void Update()
        {
            float pulse = 0.6f + 0.4f * Mathf.PerlinNoise(Time.time * glowSpeed, 0f);
            block.SetColor("_EmissionColor", glowColor * (pulse * 2f));
            cubeRenderer.SetPropertyBlock(block);
        }

        // 닿아 있는 동안 계속 확인하지만, 스턴 직후 무적 시간은 PlayerController가 막는다
        void OnCollisionEnter(Collision collision) { TryStun(collision); }
        void OnCollisionStay(Collision collision) { TryStun(collision); }

        void TryStun(Collision collision)
        {
            PlayerController player = collision.collider.GetComponent<PlayerController>();
            if (player != null) player.Stun(Random.Range(minStun, maxStun), transform.position);
        }
    }
}
