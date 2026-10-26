using UnityEngine;

namespace DodgeBall
{
    // 공이 떨어질 자리에 놓이는 원. 공이 나타나기 전에는 작게 깜빡이고,
    // 공이 가까워질수록(progress 0→1) 커지고 노란색에서 빨간색으로 변한다.
    public class LandingIndicator : MonoBehaviour
    {
        public float minSize = 0.3f;
        public float maxSize = 1.2f;
        public Color farColor = new Color(1f, 0.85f, 0.2f);
        public Color nearColor = new Color(1f, 0.15f, 0.1f);
        public float blinkSpeed = 12f;

        Renderer indicatorRenderer;
        MaterialPropertyBlock block;
        float progress;
        bool tracking;

        void Awake()
        {
            indicatorRenderer = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
        }

        public void SetProgress(float value)
        {
            tracking = true;
            progress = Mathf.Clamp01(value);
        }

        void Update()
        {
            // 공이 생기기 전 경고 단계: 작은 원이 깜빡인다
            float blink = tracking ? 0f : (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.08f;
            float size = Mathf.Lerp(minSize, maxSize, progress) + blink;
            transform.localScale = new Vector3(size, transform.localScale.y, size);

            block.SetColor("_BaseColor", Color.Lerp(farColor, nearColor, progress));
            indicatorRenderer.SetPropertyBlock(block);
        }
    }
}
