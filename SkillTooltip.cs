using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BattleSystem
{
    public class SkillTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI引用")]
        public TextMeshProUGUI descriptionText;

        [Header("显示设置")]
        public float showDelay = 0.1f;
        public Vector2 offset = new Vector2(20, -20);

        [HideInInspector] public string skillDescription;
        [HideInInspector] public string skillDetails;

        private bool _isPointerOver = false;
        private float _pointerEnterTime = 0f;
        private RectTransform _descriptionRect;

        void Start()
        {
            if (descriptionText != null)
            {
                // 注释掉颜色设置，允许在Inspector中自由修改
                // descriptionText.color = BattleConstants.TEXT_DEFAULT_COLOR;
                descriptionText.gameObject.SetActive(false);
                _descriptionRect = descriptionText.GetComponent<RectTransform>();
            }
        }

        void Update()
        {
            if (_isPointerOver && descriptionText != null && !descriptionText.gameObject.activeSelf)
            {
                if (Time.time - _pointerEnterTime >= showDelay)
                {
                    ShowTooltip();
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isPointerOver = true;
            _pointerEnterTime = Time.time;

            if (descriptionText != null && !string.IsNullOrEmpty(skillDescription))
            {
                ShowTooltip();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isPointerOver = false;
            HideTooltip();
        }

        private void ShowTooltip()
        {
            if (descriptionText == null || string.IsNullOrEmpty(skillDescription)) return;

            // 设置文本，不再使用固定的HTML标签
            string fullText = skillDescription;
            if (!string.IsNullOrEmpty(skillDetails))
                fullText += $"\n{skillDetails}";

            descriptionText.text = fullText;
            descriptionText.gameObject.SetActive(true);

            // 定位到鼠标位置
            if (_descriptionRect != null)
            {
                Vector2 mousePos = Input.mousePosition;
                _descriptionRect.position = mousePos + offset;

                // 确保不会超出屏幕
                Vector3[] corners = new Vector3[4];
                _descriptionRect.GetWorldCorners(corners);

                float width = corners[2].x - corners[0].x;
                float height = corners[1].y - corners[0].y;

                if (mousePos.x + offset.x + width > Screen.width)
                {
                    _descriptionRect.position = new Vector2(mousePos.x - offset.x - width, _descriptionRect.position.y);
                }

                if (mousePos.y + offset.y - height < 0)
                {
                    _descriptionRect.position = new Vector2(_descriptionRect.position.x, mousePos.y + offset.y + height);
                }
            }
        }

        private void HideTooltip()
        {
            if (descriptionText != null)
                descriptionText.gameObject.SetActive(false);
        }

        void OnDisable()
        {
            HideTooltip();
            _isPointerOver = false;
        }
    }
}