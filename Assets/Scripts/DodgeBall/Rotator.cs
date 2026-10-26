using UnityEngine;

namespace DodgeBall
{
    // Y축 기준으로 초당 speed도 회전
    public class Rotator : MonoBehaviour
    {
        public float speed = 90f;

        void Update()
        {
            transform.Rotate(0f, speed * Time.deltaTime, 0f, Space.World);
        }
    }
}
