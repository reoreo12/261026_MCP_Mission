using UnityEngine;

namespace DodgeBall
{
    // 공이 떨어진 자리에 먼지 파티클을 터뜨린다. 파티클 하나를 재사용해 위치만 바꿔 Emit한다.
    [RequireComponent(typeof(ParticleSystem))]
    public class DustEffect : MonoBehaviour
    {
        public static DustEffect Instance { get; private set; }

        public int minCount = 8;
        public int maxCount = 20;

        ParticleSystem particles;

        void Awake()
        {
            Instance = this;
            particles = GetComponent<ParticleSystem>();
        }

        // strength: 0~1, 세게 떨어질수록 먼지가 많다
        public void Play(Vector3 position, float strength)
        {
            var emitParams = new ParticleSystem.EmitParams { position = position, applyShapeToPosition = true };
            particles.Emit(emitParams, Mathf.RoundToInt(Mathf.Lerp(minCount, maxCount, strength)));
        }
    }
}
