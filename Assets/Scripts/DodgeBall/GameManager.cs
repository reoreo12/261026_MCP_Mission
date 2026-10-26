using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DodgeBall
{
    // 생존 시간 측정·표시와 Game Over 상태, Game Over 화면과 재시작을 관리한다.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public Text timeText;

        [Header("Game Over UI")]
        public CanvasGroup gameOverPanel;
        public Text finalTimeText;
        public Text bestTimeText;
        [Tooltip("이번 기록이 최고 기록을 갱신했을 때만 켜지는 강조 표시")]
        public RectTransform newBestBadge;
        public Button restartButton;
        public float panelFadeTime = 0.4f;

        public bool IsGameOver { get; private set; }
        public float SurvivalTime { get; private set; }
        public bool IsNewBest { get; private set; }

        // 게임을 껐다 켜도 남도록 PlayerPrefs에 저장한다
        public const string BestTimeKey = "DodgeBall.BestTime";

        int shownSeconds = -1;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            gameOverPanel.alpha = 0f;
            gameOverPanel.interactable = false;
            gameOverPanel.blocksRaycasts = false;
            newBestBadge.gameObject.SetActive(false);
            restartButton.onClick.AddListener(Restart);
        }

        void Update()
        {
            if (IsGameOver)
            {
                gameOverPanel.alpha = Mathf.MoveTowards(gameOverPanel.alpha, 1f, Time.deltaTime / panelFadeTime);
                if (IsNewBest) newBestBadge.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.unscaledTime * 6f));
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && keyboard.rKey.wasPressedThisFrame) Restart();
                return;
            }
            SurvivalTime += Time.deltaTime;
            UpdateText();
        }

        public void GameOver()
        {
            if (IsGameOver) return;
            IsGameOver = true;
            Debug.Log("Game Over");

            float best = PlayerPrefs.GetFloat(BestTimeKey, 0f);
            IsNewBest = SurvivalTime > best;
            if (IsNewBest)
            {
                best = SurvivalTime;
                PlayerPrefs.SetFloat(BestTimeKey, best);
                PlayerPrefs.Save();
            }

            finalTimeText.text = "Survival Time: " + SurvivalTime.ToString("0.0") + "s";
            bestTimeText.text = "Best: " + best.ToString("0.0") + "s";
            newBestBadge.gameObject.SetActive(IsNewBest);
            gameOverPanel.interactable = true;
            gameOverPanel.blocksRaycasts = true;
        }

        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
