using System.Collections;
using UnityEngine;

namespace DodgeBall
{
    // interval초마다 바닥 위 공중의 랜덤 위치에 공을 떨어뜨린다.
    // 공보다 먼저 착지 지점에 경고 원을 warningTime초 동안 보여 준다.
    public class SphereSpawner : MonoBehaviour
    {
        public float interval = 3f;
        public float spawnHeight = 8f;
        public float areaHalfSize = 4.5f;
        public float sphereLifetime = 8f;
        public float sphereMass = 5f;
        public Material sphereMaterial;

        [Header("Landing Warning")]
        public float warningTime = 0.8f;
        public Material indicatorMaterial;

        float timer;

        void Update()
        {
            if (GameManager.Instance.IsGameOver) return;

            timer += Time.deltaTime;
            if (timer < interval) return;
            timer -= interval;

            Vector3 position = new Vector3(Random.Range(-areaHalfSize, areaHalfSize), spawnHeight, Random.Range(-areaHalfSize, areaHalfSize));
            StartCoroutine(WarnThenDrop(position));
        }

        IEnumerator WarnThenDrop(Vector3 position)
        {
            LandingIndicator indicator = CreateIndicator(position, out float landingHeight);
            yield return new WaitForSeconds(warningTime);

            if (GameManager.Instance.IsGameOver)
            {
                Destroy(indicator.gameObject);
                yield break;
            }
            Spawn(position, indicator, landingHeight);
        }

        public GameObject Spawn(Vector3 position)
        {
            LandingIndicator indicator = CreateIndicator(position, out float landingHeight);
            return Spawn(position, indicator, landingHeight);
        }

        GameObject Spawn(Vector3 position, LandingIndicator indicator, float landingHeight)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Obstacle Sphere";
            sphere.tag = "Obstacle";
            sphere.transform.position = position;
            sphere.GetComponent<Renderer>().sharedMaterial = sphereMaterial;
            sphere.AddComponent<Rigidbody>().mass = sphereMass;
            sphere.AddComponent<FallingBall>().Init(indicator, landingHeight);
            // 바닥 밖으로 굴러 떨어진 공이 계속 쌓이지 않도록 일정 시간 뒤 제거
            Destroy(sphere, sphereLifetime);
            return sphere;
        }

        // 공 아래로 레이를 쏴서 실제로 닿을 면(바닥 또는 큐브 윗면)에 경고 원을 놓는다
        LandingIndicator CreateIndicator(Vector3 position, out float landingHeight)
        {
            landingHeight = 0f;
            Vector3 point = new Vector3(position.x, 0f, position.z);
            // 플레이어나 다른 공처럼 움직이는 물체(Rigidbody)는 건너뛰고 가장 가까운 고정 면을 고른다
            float nearest = float.PositiveInfinity;
            foreach (RaycastHit hit in Physics.RaycastAll(position, Vector3.down, position.y + 1f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.attachedRigidbody != null || hit.distance >= nearest) continue;
                nearest = hit.distance;
                point = hit.point;
                landingHeight = hit.point.y;
            }

            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Landing Indicator";
            Destroy(disc.GetComponent<Collider>());
            disc.transform.position = point + Vector3.up * 0.02f;
            disc.transform.localScale = new Vector3(0.3f, 0.01f, 0.3f);
            Renderer discRenderer = disc.GetComponent<Renderer>();
            discRenderer.sharedMaterial = indicatorMaterial;
            discRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return disc.AddComponent<LandingIndicator>();
        }
    }
}
