using UnityEngine;

namespace DodgeBall
{
    // interval초마다 바닥 위 공중의 랜덤 위치에 Rigidbody Sphere를 만들어 떨어뜨린다.
    public class SphereSpawner : MonoBehaviour
    {
        public float interval = 3f;
        public float spawnHeight = 8f;
        public float areaHalfSize = 4.5f;
        public float sphereLifetime = 8f;
        public Material sphereMaterial;

        float timer;

        void Update()
        {
            if (GameManager.Instance.IsGameOver) return;

            timer += Time.deltaTime;
            if (timer < interval) return;
            timer -= interval;

            Spawn(new Vector3(Random.Range(-areaHalfSize, areaHalfSize), spawnHeight, Random.Range(-areaHalfSize, areaHalfSize)));
        }

        public GameObject Spawn(Vector3 position)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Obstacle Sphere";
            sphere.tag = "Obstacle";
            sphere.transform.position = position;
            sphere.GetComponent<Renderer>().sharedMaterial = sphereMaterial;
            sphere.AddComponent<Rigidbody>();
            // 바닥 밖으로 굴러 떨어진 공이 계속 쌓이지 않도록 일정 시간 뒤 제거
            Destroy(sphere, sphereLifetime);
            return sphere;
        }
    }
}
