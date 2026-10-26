using UnityEngine;

namespace DodgeBall
{
    // 오브젝트 표면 위를 지지직 튀는 전기 줄기. LineRenderer 여러 개의 꺾인 선을 짧은 간격으로 다시 그린다.
    public class ElectricArcs : MonoBehaviour
    {
        public Material arcMaterial;
        public int arcCount = 3;
        public int segments = 8;
        public float jitter = 0.15f;
        public float width = 0.04f;
        public float refreshInterval = 0.05f;
        [Tooltip("전기가 흐를 표면의 반지름 (로컬 좌표)")]
        public Vector3 extents = new Vector3(0.55f, 0.55f, 0.55f);

        LineRenderer[] arcs;
        float timer;

        void Awake()
        {
            arcs = new LineRenderer[arcCount];
            for (int i = 0; i < arcCount; i++)
            {
                var line = new GameObject("Arc" + i).AddComponent<LineRenderer>();
                line.transform.SetParent(transform, false);
                line.useWorldSpace = false;
                line.positionCount = segments;
                line.widthMultiplier = width;
                line.sharedMaterial = arcMaterial;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                arcs[i] = line;
            }
        }

        void OnEnable()
        {
            timer = 0f;
        }

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = refreshInterval;

            foreach (LineRenderer arc in arcs)
            {
                // 가끔 꺼져서 깜빡이는 느낌을 준다
                arc.enabled = Random.value > 0.25f;
                Vector3 from = RandomSurfacePoint();
                Vector3 to = RandomSurfacePoint();
                for (int s = 0; s < segments; s++)
                {
                    Vector3 point = Vector3.Lerp(from, to, s / (segments - 1f));
                    if (s > 0 && s < segments - 1) point += Random.insideUnitSphere * jitter;
                    arc.SetPosition(s, point);
                }
            }
        }

        Vector3 RandomSurfacePoint()
        {
            var point = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            int axis = Random.Range(0, 3);
            point[axis] = point[axis] < 0f ? -1f : 1f;
            return Vector3.Scale(point, extents);
        }
    }
}
