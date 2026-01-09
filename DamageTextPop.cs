using TMPro;
using UnityEngine;

namespace BattleSystem
{
    public class DamageTextPop : MonoBehaviour
    {
        public enum DamageType
        {
            Normal,
            Critical,
            SuperEffective,
            Weak,
            Heal,
            Status
        }

        [Header("基本设置")]
        public float moveSpeed = 50f;
        public float fadeSpeed = 0.5f;
        public float lifeTime = 2.0f;

        [Header("效果设置")]
        public float normalScale = 1.5f;
        public Color normalColor = Color.red;
        public float critScale = 2.5f;
        public Color critColor = new Color(1f, 0.2f, 0f);
        public float superScale = 2.2f;
        public Color superColor = new Color(1f, 0.5f, 0f);
        public Color weakColor = Color.gray;
        public float weakScale = 0.8f;
        public Color healColor = Color.green;  // 确保这里是绿色
        public Color statusColor = Color.cyan;

        [Header("治疗特效")]
        public AnimationCurve healScaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 1.5f);
        public float healPulseSpeed = 3f;

        private TextMeshProUGUI _textMesh;
        private Vector3 _originalScale;
        private float _timer = 0f;
        private DamageType _currentType;
        private Vector3 _originalPosition;

        void Awake()
        {
            _textMesh = GetComponent<TextMeshProUGUI>();
            if (_textMesh == null)
            {
                Debug.LogError("DamageTextPop: 缺少TextMeshProUGUI组件!");
                return;
            }

            _originalScale = transform.localScale;
            _originalPosition = transform.position;

            // 确保治疗颜色是绿色
            if (healColor == Color.green)
            {
                Debug.Log("DamageTextPop: 治疗颜色已设置为绿色");
            }
            else
            {
                Debug.LogWarning($"DamageTextPop: 治疗颜色不是纯绿色，当前值: {healColor}");
                // 强制设置为绿色
                healColor = Color.green;
            }
        }

        public void ShowDamage(int amount, DamageType type = DamageType.Normal)
        {
            _currentType = type;
            _timer = 0f;

            if (_textMesh == null)
            {
                Debug.LogError("DamageTextPop: _textMesh为空!");
                return;
            }

            // 保存初始位置用于抖动效果
            _originalPosition = transform.position;

            string prefix = GetPrefixForType(type);

            if (type == DamageType.Heal)
            {
                // 治疗显示为 +XX 绿色
                _textMesh.text = $"+{amount}";
                _textMesh.color = healColor;
                Debug.Log($"DamageTextPop: 显示治疗数字 +{amount}, 颜色: {healColor}");
            }
            else if (amount <= 0)
            {
                _textMesh.text = prefix;
                _textMesh.color = GetColorForType(type);
            }
            else
            {
                _textMesh.text = $"{prefix} {amount}";
                _textMesh.color = GetColorForType(type);
            }

            // 设置缩放
            transform.localScale = _originalScale * GetScaleForType(type);

            // 设置字体大小
            if (type == DamageType.Critical)
            {
                _textMesh.fontSize = 48f;
            }
            else if (type == DamageType.Heal)
            {
                _textMesh.fontSize = 40f; // 治疗字体稍大
            }
            else
            {
                _textMesh.fontSize = 36f;
            }

            // 暴击时添加抖动效果
            if (type == DamageType.Critical)
            {
                StartCoroutine(CriticalShake());
            }
            // 治疗时添加脉冲效果
            else if (type == DamageType.Heal)
            {
                StartCoroutine(HealPulseEffect());
                Debug.Log("DamageTextPop: 启动治疗脉冲效果");
            }

            // 自动销毁
            Destroy(gameObject, lifeTime);

            Debug.Log($"DamageTextPop: 显示数字类型: {type}, 数值: {amount}, 颜色: {_textMesh.color}");
        }

        void Update()
        {
            if (_textMesh == null) return;

            _timer += Time.deltaTime;

            // 向上移动
            transform.position += new Vector3(0, moveSpeed * Time.deltaTime, 0);

            // 淡出效果
            if (_timer > lifeTime - 0.5f)
            {
                Color color = _textMesh.color;
                float fadeProgress = (_timer - (lifeTime - 0.5f)) / 0.5f;
                color.a = Mathf.Lerp(1f, 0f, fadeProgress);
                _textMesh.color = color;
            }
        }

        private System.Collections.IEnumerator CriticalShake()
        {
            float duration = 0.3f;
            float elapsed = 0f;
            Vector3 originalPos = _originalPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float shakeAmount = Mathf.Lerp(10f, 0f, elapsed / duration);
                transform.position = originalPos + new Vector3(
                    Random.Range(-shakeAmount, shakeAmount) * Time.deltaTime * 10f,
                    Random.Range(-shakeAmount, shakeAmount) * Time.deltaTime * 10f,
                    0
                );
                yield return null;
            }

            transform.position = originalPos;
        }

        private System.Collections.IEnumerator HealPulseEffect()
        {
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scaleMultiplier = healScaleCurve.Evaluate(t);
                transform.localScale = _originalScale * scaleMultiplier;

                // 轻微的颜色闪烁
                if (_textMesh != null)
                {
                    float colorPulse = Mathf.Sin(elapsed * healPulseSpeed) * 0.2f + 0.8f;
                    Color color = healColor;
                    color.r = Mathf.Clamp(color.r * colorPulse, 0.5f, 1f);
                    color.g = Mathf.Clamp(color.g * colorPulse, 0.8f, 1f);
                    _textMesh.color = color;
                }

                yield return null;
            }

            // 恢复到原始大小
            transform.localScale = _originalScale;
        }

        private string GetPrefixForType(DamageType type)
        {
            switch (type)
            {
                case DamageType.Critical:
                    return "暴击!";
                case DamageType.SuperEffective:
                    return "效果绝佳!";
                case DamageType.Weak:
                    return "效果一般";
                case DamageType.Heal:
                    return "+";
                case DamageType.Status:
                    return "状态";
                case DamageType.Normal:
                default:
                    return "-";
            }
        }

        private Color GetColorForType(DamageType type)
        {
            switch (type)
            {
                case DamageType.Critical:
                    return critColor;
                case DamageType.SuperEffective:
                    return superColor;
                case DamageType.Weak:
                    return weakColor;
                case DamageType.Heal:
                    return healColor; // 确保返回的是绿色
                case DamageType.Status:
                    return statusColor;
                case DamageType.Normal:
                default:
                    return normalColor;
            }
        }

        private float GetScaleForType(DamageType type)
        {
            switch (type)
            {
                case DamageType.Critical:
                    return critScale;
                case DamageType.SuperEffective:
                    return superScale;
                case DamageType.Weak:
                    return weakScale;
                case DamageType.Heal:
                    return 1.0f;
                case DamageType.Status:
                    return 1.0f;
                case DamageType.Normal:
                default:
                    return normalScale;
            }
        }

        // 检查颜色设置（编辑器中使用）
        void OnValidate()
        {
#if UNITY_EDITOR
            // 在编辑器中确保治疗颜色是绿色
            if (healColor != Color.green)
            {
                Debug.LogWarning("DamageTextPop: 治疗颜色不是绿色，已自动修正");
                healColor = Color.green;
            }
#endif
        }
    }
}