using UnityEngine;
using UnityEngine.UI;

namespace DodgeBall
{
    // 생존 시간 측정·표시와 Game Over 상태를 관리한다.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public Text timeText;

        public bool IsGameOver { get; private set; }
        public float SurvivalTime { get; private set; }

        int shownSeconds = -1;

        void Awake()
        {
            Instance = this;
        }

        void Update()
        {
            if (IsGameOver) return;
            SurvivalTime += Time.deltaTime;
            UpdateText();
        }

        public void GameOver()
        {
            if (IsGameOver) return;
            IsGameOver = true;
            Debug.Log("Game Over");
        }

        void UpdateText()
        {
            // 표시되는 초가 바뀔 때만 문자열을 새로 만든다(매 프레임 GC 할당 방지)
            int seconds = Mathf.FloorToInt(SurvivalTime);
            if (seconds == shownSeconds) return;
            shownSeconds = seconds;
            timeText.text = "Survival Time: " + seconds + "s";
        }
    }
}
