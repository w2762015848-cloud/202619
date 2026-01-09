using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace BattleSystem
{
    [RequireComponent(typeof(PetEntity))]
    public class PetUI : MonoBehaviour
    {
        [Header("UI引用")]
        public Slider hpSlider;
        public Image hpFillImage;
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI levelText;

        [Header("颜色设置")]
        public Color hpHighColor = Color.green;
        public Color hpMediumColor = Color.yellow;
        public Color hpLowColor = Color.red;

        private PetEntity _petEntity;
        private Coroutine _hpAnimation;

        void Awake()
        {
            _petEntity = GetComponent<PetEntity>();

            // 自动查找UI组件（如果未分配）
            if (hpSlider == null) hpSlider = GetComponentInChildren<Slider>();
            if (hpText == null) hpText = FindTextComponent("HP", "血");
            if (nameText == null) nameText = FindTextComponent("Name", "名字");
        }

        void Start()
        {
            InitializeUI();
            SubscribeToEvents();
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private TextMeshProUGUI FindTextComponent(params string[] keywords)
        {
            var texts = GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                foreach (var keyword in keywords)
                {
                    if (text.name.Contains(keyword))
                    {
                        return text;
                    }
                }
            }
            return null;
        }

        private void SubscribeToEvents()
        {
            BattleEvents.OnPetHPChanged += OnPetHPChanged;
            BattleEvents.OnPetUIUpdateNeeded += OnPetUIUpdateNeeded;
            // 注意：移除了状态效果相关的事件订阅
        }

        private void UnsubscribeFromEvents()
        {
            BattleEvents.OnPetHPChanged -= OnPetHPChanged;
            BattleEvents.OnPetUIUpdateNeeded -= OnPetUIUpdateNeeded;
            // 注意：移除了状态效果相关的事件订阅
        }

        private void InitializeUI()
        {
            if (nameText != null && _petEntity != null)
            {
                nameText.text = _petEntity.petName;
            }

            if (hpSlider != null)
            {
                hpSlider.maxValue = _petEntity.MaxHP;
                hpSlider.value = _petEntity.CurrentHP;
            }

            UpdateHPDisplay();
        }

        private void OnPetHPChanged(PetEntity pet, int currentHP, int maxHP)
        {
            if (pet != _petEntity) return;
            UpdateHPDisplay();
        }

        private void OnPetUIUpdateNeeded(PetEntity pet)
        {
            if (pet != _petEntity) return;
            UpdateHPDisplay();
        }

        private void UpdateHPDisplay()
        {
            // 更新HP文本
            if (hpText != null)
            {
                hpText.text = $"{_petEntity.CurrentHP}/{_petEntity.MaxHP}";
            }

            // 更新HP条颜色
            UpdateHPBarColor();

            // 动画更新HP条
            if (hpSlider != null)
            {
                if (_hpAnimation != null)
                {
                    StopCoroutine(_hpAnimation);
                }
                _hpAnimation = StartCoroutine(AnimateHPBar(_petEntity.CurrentHP));
            }
        }

        private System.Collections.IEnumerator AnimateHPBar(float targetValue)
        {
            if (hpSlider == null) yield break;

            float startValue = hpSlider.value;
            float elapsed = 0f;
            float duration = BattleConstants.HP_ANIMATION_DURATION;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                hpSlider.value = Mathf.Lerp(startValue, targetValue, t);
                yield return null;
            }

            hpSlider.value = targetValue;
            _hpAnimation = null;
        }

        private void UpdateHPBarColor()
        {
            if (hpFillImage == null || _petEntity.MaxHP <= 0) return;

            float hpPercent = (float)_petEntity.CurrentHP / _petEntity.MaxHP;

            if (hpPercent <= 0.3f)
                hpFillImage.color = hpLowColor;
            else if (hpPercent <= 0.6f)
                hpFillImage.color = hpMediumColor;
            else
                hpFillImage.color = hpHighColor;
        }

        void OnValidate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && hpSlider != null)
            {
                var pet = GetComponent<PetEntity>();
                if (pet != null)
                {
                    hpSlider.maxValue = pet.MaxHP;
                    hpSlider.value = pet.CurrentHP;
                    UpdateHPBarColor();
                }
            }
#endif
        }
    }
}