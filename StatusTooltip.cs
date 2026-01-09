using TMPro;
using UnityEngine;

namespace BattleSystem
{
    public class StatusTooltip : MonoBehaviour
    {
        [Header("UI组件")]
        public TextMeshProUGUI tooltipText;
        public RectTransform tooltipRect;
        public CanvasGroup canvasGroup;

        [Header("显示设置")]
        public float fadeInSpeed = 10f;
        public float fadeOutSpeed = 5f;
        public Vector2 offset = new Vector2(0, 60); // 默认向上偏移

        private bool _isShowing = false;
        private string _currentText = "";

        void Awake()
        {
            // 自动获取组件
            if (tooltipText == null)
                tooltipText = GetComponentInChildren<TextMeshProUGUI>();

            if (tooltipRect == null)
                tooltipRect = GetComponent<RectTransform>();

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            // 初始隐藏
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        void Update()
        {
            // 淡入淡出效果
            if (canvasGroup != null)
            {
                float targetAlpha = _isShowing ? 1f : 0f;
                float currentAlpha = canvasGroup.alpha;
                float speed = _isShowing ? fadeInSpeed : fadeOutSpeed;

                if (Mathf.Abs(currentAlpha - targetAlpha) > 0.01f)
                {
                    canvasGroup.alpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * speed);
                }
                else
                {
                    canvasGroup.alpha = targetAlpha;
                }

                // 完全隐藏后销毁
                if (!_isShowing && canvasGroup.alpha <= 0.01f)
                {
                    Destroy(gameObject);
                }
            }
        }

        public void SetText(string text)
        {
            _currentText = text;
            if (tooltipText != null)
            {
                tooltipText.text = text;
            }
        }

        public void SetPosition(Vector2 anchoredPosition)
        {
            if (tooltipRect != null)
            {
                tooltipRect.anchoredPosition = anchoredPosition;
                AdjustPositionToStayOnScreen();
            }
        }

        private void AdjustPositionToStayOnScreen()
        {
            if (tooltipRect == null) return;

            // 获取父Canvas
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect == null) return;

            Vector2 canvasSize = canvasRect.rect.size;
            Vector2 tooltipSize = tooltipRect.rect.size;
            Vector2 anchoredPos = tooltipRect.anchoredPosition;

            // 检查是否超出右侧边界
            float rightEdge = anchoredPos.x + tooltipSize.x / 2;
            if (rightEdge > canvasSize.x / 2)
            {
                anchoredPos.x = canvasSize.x / 2 - tooltipSize.x / 2 - 10;
            }

            // 检查是否超出左侧边界
            float leftEdge = anchoredPos.x - tooltipSize.x / 2;
            if (leftEdge < -canvasSize.x / 2)
            {
                anchoredPos.x = -canvasSize.x / 2 + tooltipSize.x / 2 + 10;
            }

            // 检查是否超出上侧边界
            float topEdge = anchoredPos.y + tooltipSize.y / 2;
            if (topEdge > canvasSize.y / 2)
            {
                anchoredPos.y = canvasSize.y / 2 - tooltipSize.y / 2 - 10;
            }

            tooltipRect.anchoredPosition = anchoredPos;
        }

        public void Show()
        {
            _isShowing = true;
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false; // 工具提示不应该阻挡射线
            }
        }

        public void Hide()
        {
            _isShowing = false;
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            if (active)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }
    }
}