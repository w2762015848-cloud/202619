using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace BattleSystem
{
    public class BattleUIController : MonoBehaviour
    {
        [Header("技能按钮")]
        public Button[] skillButtons;
        public TextMeshProUGUI[] skillButtonTexts;
        public Image[] skillButtonIcons;

        [Header("UI引用")]
        public RectTransform damageCanvas;
        public GameObject damageTextPrefab;

        [Header("工具提示")]
        public TextMeshProUGUI descriptionTooltipText;

        [Header("颜色设置")]
        public Color physicalColor = BattleConstants.PHYSICAL_COLOR;
        public Color specialColor = BattleConstants.SPECIAL_COLOR;
        public Color statusColor = BattleConstants.STATUS_COLOR;

        private List<SkillTooltip> _skillTooltips = new List<SkillTooltip>();
        private ObjectPool<DamageTextPop> _damageTextPool;

        void Awake()
        {
            InitializeUI();
            InitializeObjectPool();
            SubscribeToEvents();
            AutoBindButtonEvents();
        }

        void Start()
        {
            InitializeSkillTooltips();
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeUI()
        {
            if (descriptionTooltipText != null)
            {
                descriptionTooltipText.gameObject.SetActive(false);
            }
        }

        private void AutoBindButtonEvents()
        {
            if (skillButtons == null)
            {
                Debug.LogWarning("AutoBindButtonEvents: skillButtons数组为空");
                return;
            }

            for (int i = 0; i < skillButtons.Length; i++)
            {
                Button btn = skillButtons[i];
                if (btn == null) continue;

                btn.onClick.RemoveAllListeners();
                int skillIndex = i;

                btn.onClick.AddListener(() =>
                {
                    BattleController controller = BattleController.Instance;
                    if (controller != null)
                    {
                        Debug.Log($"点击按钮{skillIndex}，调用技能索引{skillIndex}");
                        controller.OnPlayerUseSkill(skillIndex);
                    }
                    else
                    {
                        Debug.LogWarning($"BattleController.Instance为空，无法调用技能{skillIndex}");
                    }
                });

                Debug.Log($"自动绑定按钮{i}到技能索引{skillIndex}");
            }
        }

        private void InitializeObjectPool()
        {
            if (damageTextPrefab != null && damageCanvas != null)
            {
                _damageTextPool = new ObjectPool<DamageTextPop>(
                    createFunc: () => Instantiate(damageTextPrefab, damageCanvas).GetComponent<DamageTextPop>(),
                    actionOnGet: (obj) => obj.gameObject.SetActive(true),
                    actionOnRelease: (obj) => obj.gameObject.SetActive(false),
                    actionOnDestroy: (obj) => Destroy(obj.gameObject),
                    defaultCapacity: 10,
                    maxSize: 50
                );
            }
            else
            {
                Debug.LogWarning("DamageTextPrefab或DamageCanvas未设置，对象池未初始化");
            }
        }

        // 【按照你的逻辑修复】添加对OnHealReceived的监听
        private void SubscribeToEvents()
        {
            BattleEvents.OnSkillButtonsUpdateNeeded += OnSkillButtonsUpdateNeeded;
            BattleEvents.OnDamageDealt += OnDamageDealt;
            // 【新增】监听治疗事件
            BattleEvents.OnHealReceived += OnHealReceived;
        }

        // 【按照你的逻辑修复】取消监听
        private void UnsubscribeFromEvents()
        {
            BattleEvents.OnSkillButtonsUpdateNeeded -= OnSkillButtonsUpdateNeeded;
            BattleEvents.OnDamageDealt -= OnDamageDealt;
            // 【新增】取消监听
            BattleEvents.OnHealReceived -= OnHealReceived;
        }

        // 初始化UI
        public void InitializeUI(PetEntity playerPet)
        {
            if (playerPet != null)
            {
                UpdateSkillButtons(playerPet);
            }
            else
            {
                Debug.LogWarning("InitializeUI: playerPet为空");
            }
        }

        // 更新技能按钮
        private void OnSkillButtonsUpdateNeeded(PetEntity pet)
        {
            UpdateSkillButtons(pet);
        }

        public void UpdateSkillButtons(PetEntity playerPet)
        {
            if (playerPet == null)
            {
                Debug.LogError("UpdateSkillButtons: playerPet is null!");
                return;
            }

            if (skillButtons == null)
            {
                Debug.LogError("UpdateSkillButtons: skillButtons array is null!");
                return;
            }

            int skillCount = playerPet.skills != null ? playerPet.skills.Count : 0;
            Debug.Log($"更新技能按钮: 宠物有 {skillCount} 个技能, UI有 {skillButtons.Length} 个按钮");

            for (int i = 0; i < skillButtons.Length; i++)
            {
                if (skillButtons[i] == null)
                {
                    Debug.LogWarning($"技能按钮 {i} 为空!");
                    continue;
                }

                if (i < skillCount && playerPet.skills[i] != null)
                {
                    SkillData skill = playerPet.skills[i];

                    skillButtons[i].gameObject.SetActive(true);
                    skillButtons[i].interactable = (skill.currentPP > 0);

                    if (skillButtonTexts != null && i < skillButtonTexts.Length)
                    {
                        TextMeshProUGUI textComp = skillButtonTexts[i];
                        if (textComp != null)
                        {
                            string categoryTag = GetCategoryTag(skill.category);
                            string displayText = $"{skill.skillName}\n";

                            if (skill.power > 0)
                            {
                                displayText += $"威力: {skill.power}\n";
                            }

                            if (skill.maxPP > 0)
                            {
                                if (skill.currentPP <= 0)
                                    displayText += $"<color=red>({skill.currentPP}/{skill.maxPP})</color>";
                                else
                                    displayText += $"<color=#FFD700>({skill.currentPP}/{skill.maxPP})</color>";
                            }

                            if (!string.IsNullOrEmpty(skill.element) && skill.element != "无")
                            {
                                displayText += $"\n[{skill.element}] {categoryTag}";
                            }
                            else
                            {
                                displayText += $"\n{categoryTag}";
                            }

                            textComp.text = displayText;
                        }
                    }

                    if (skillButtonIcons != null && i < skillButtonIcons.Length && skillButtonIcons[i] != null)
                    {
                        if (skill.icon != null)
                        {
                            skillButtonIcons[i].sprite = skill.icon;
                        }
                    }

                    if (_skillTooltips != null && i < _skillTooltips.Count && _skillTooltips[i] != null)
                    {
                        _skillTooltips[i].skillDescription = skill.description;
                        _skillTooltips[i].skillDetails = $"威力: {skill.power} | 命中: {skill.accuracy} | PP: {skill.currentPP}/{skill.maxPP}";
                    }
                }
                else
                {
                    skillButtons[i].gameObject.SetActive(true);
                    skillButtons[i].interactable = false;

                    if (skillButtonTexts != null && i < skillButtonTexts.Length && skillButtonTexts[i] != null)
                    {
                        skillButtonTexts[i].text = "无技能";
                        skillButtonTexts[i].color = Color.gray;
                    }
                }
            }

            Debug.Log("技能按钮更新完成");
        }

        // 设置技能按钮交互状态
        public void SetSkillButtonsInteractable(bool interactable)
        {
            if (skillButtons == null)
            {
                Debug.LogWarning("SetSkillButtonsInteractable: skillButtons 数组为空!");
                return;
            }

            foreach (Button btn in skillButtons)
            {
                if (btn != null && btn.gameObject.activeSelf)
                {
                    btn.interactable = interactable;
                }
            }

            Debug.Log($"技能按钮交互状态设置为: {interactable}");
        }

        // 处理伤害显示
        private void OnDamageDealt(PetEntity attacker, PetEntity target, SkillData skill, int damage)
        {
            if (target == null)
            {
                Debug.LogWarning("OnDamageDealt: 目标为空");
                return;
            }

            if (damageCanvas == null)
            {
                Debug.LogWarning("OnDamageDealt: DamageCanvas为空");
                return;
            }

            if (_damageTextPool == null)
            {
                Debug.LogWarning("OnDamageDealt: 伤害数字对象池未初始化");
                return;
            }

            DamageTextPop damageText = _damageTextPool.Get();

            if (damageText != null)
            {
                RectTransform targetRect = target.GetComponent<RectTransform>();
                if (targetRect != null)
                {
                    Vector3 targetPosition = targetRect.position;
                    Vector3 offset = new Vector3(
                        Random.Range(-50f, 50f),
                        Random.Range(50f, 100f),
                        0
                    );
                    damageText.transform.position = targetPosition + offset;
                }
                else
                {
                    damageText.transform.position = new Vector3(Screen.width / 2, Screen.height / 2, 0);
                }

                DamageTextPop.DamageType damageType = DamageTextPop.DamageType.Normal;

                if (skill != null && skill.effectType == SkillEffectType.GuaranteedCrit)
                {
                    damageType = DamageTextPop.DamageType.Critical;
                }

                damageText.ShowDamage(damage, damageType);

                StartCoroutine(ReturnToPoolAfterDelay(damageText, 3f));

                Debug.Log($"在 {target.petName} 位置显示伤害数字: {damage}");
            }
        }

        // 【按照你的逻辑修复】处理治疗数字显示的方法
        private void OnHealReceived(PetEntity target, int amount)
        {
            if (target == null || amount <= 0) return;

            // 从对象池获取飘字组件
            if (_damageTextPool != null)
            {
                DamageTextPop damageText = _damageTextPool.Get();

                // 设置位置（复用你之前的逻辑）
                RectTransform targetRect = target.GetComponent<RectTransform>();
                if (targetRect != null)
                {
                    Vector3 offset = new Vector3(Random.Range(-30f, 30f), Random.Range(50f, 80f), 0);
                    damageText.transform.position = targetRect.position + offset;
                }

                // 显示为治疗类型（绿色）
                damageText.ShowDamage(amount, DamageTextPop.DamageType.Heal);

                // 3秒后回收
                StartCoroutine(ReturnToPoolAfterDelay(damageText, 3f));
            }
        }

        private IEnumerator ReturnToPoolAfterDelay(DamageTextPop damageText, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (damageText != null && _damageTextPool != null)
            {
                _damageTextPool.Release(damageText);
            }
        }

        // 获取分类标签 - 支持中文分类
        private string GetCategoryTag(string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                return "[未知]";
            }

            string cat = category.Trim();

            if (cat.Contains("攻击") || cat == "攻击" || cat == BattleConstants.CATEGORY_PHYSICAL || cat.ToLower() == "physical")
                return "[攻击]";
            else if (cat.Contains("特攻") || cat == "特攻" || cat == BattleConstants.CATEGORY_SPECIAL || cat.ToLower() == "special")
                return "[特攻]";
            else if (cat.Contains("属性") || cat == "属性" || cat == BattleConstants.CATEGORY_STATUS ||
                     cat == "辅助" || cat == "被动" || cat.ToLower() == "status")
                return "[属性]";
            else
                return $"[{category}]";
        }

        // 初始化技能工具提示
        private void InitializeSkillTooltips()
        {
            _skillTooltips.Clear();

            if (skillButtons == null)
            {
                Debug.LogWarning("InitializeSkillTooltips: skillButtons数组为空");
                return;
            }

            for (int i = 0; i < skillButtons.Length; i++)
            {
                if (skillButtons[i] != null)
                {
                    SkillTooltip tooltip = skillButtons[i].GetComponent<SkillTooltip>();
                    if (tooltip == null)
                    {
                        tooltip = skillButtons[i].gameObject.AddComponent<SkillTooltip>();
                        Debug.Log($"为按钮{i}添加SkillTooltip组件");
                    }

                    if (descriptionTooltipText != null)
                    {
                        tooltip.descriptionText = descriptionTooltipText;
                    }

                    _skillTooltips.Add(tooltip);
                }
            }
        }
    }
}