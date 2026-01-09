using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BattleSystem
{
    public class SkillExecutor : MonoBehaviour
    {
        [Header("引用")]
        public BattleController battleController;

        [Header("设置")]
        public float statusEffectDelay = 0.5f;
        public float skillExecutionDelay = 0.3f;

        // 新增：治疗和伤害数字显示相关字段
        [Header("数字显示设置")]
        public GameObject damageTextPrefab;      // 伤害数字预制体
        public RectTransform damageCanvas;       // 伤害数字画布
        public float damageNumberYOffset = 50f;  // 数字垂直偏移

        [Header("治疗特效")]
        public GameObject healEffectPrefab;      // 治疗特效预制体（可选）
        public Color healTextColor = Color.green; // 治疗数字颜色

        void Start()
        {
            // 自动查找引用
            if (damageCanvas == null)
            {
                GameObject canvasObj = GameObject.Find("Damage_Canvas");
                if (canvasObj != null)
                {
                    damageCanvas = canvasObj.GetComponent<RectTransform>();
                    Debug.Log($"自动找到 Damage_Canvas: {damageCanvas != null}");
                }
            }

            if (damageTextPrefab == null)
            {
                // 尝试从 Resources 加载默认预制体
                damageTextPrefab = Resources.Load<GameObject>("DamageTextPrefab");
                if (damageTextPrefab != null)
                {
                    Debug.Log("从 Resources 加载 DamageTextPrefab");
                }
            }
        }

        // 执行技能
        public void ExecuteSkill(PetEntity attacker, PetEntity target, SkillData skill)
        {
            if (skill == null || attacker == null)
            {
                Debug.LogError("技能执行器: 参数为空");
                return;
            }

            StartCoroutine(ExecuteSkillCoroutine(attacker, target, skill));
        }

        // 执行技能协程
        private IEnumerator ExecuteSkillCoroutine(PetEntity attacker, PetEntity target, SkillData skill)
        {
            Debug.Log($"{attacker.petName} 使用 {skill.skillName}");

            // 短暂延迟让玩家看到技能选择
            yield return new WaitForSeconds(skillExecutionDelay);

            // 检查技能类型
            if (skill.IsStatusSkill())
            {
                ExecuteStatusSkill(attacker, target, skill);
            }
            else
            {
                ExecuteDamageSkill(attacker, target, skill);
            }
        }

        // 执行伤害技能
        private void ExecuteDamageSkill(PetEntity attacker, PetEntity target, SkillData skill)
        {
            // 检查命中
            if (!DamageCalculator.CheckHit(skill.accuracy, attacker))
            {
                ShowDamagePopup(target, 0, 1.0f, "Miss", DamageTextPop.DamageType.Normal);
                StartCoroutine(EndTurnAfterDelay(attacker));
                return;
            }

            // 检查是否会攻击自己（混乱状态）
            if (StatusEffectManager.Instance != null &&
                StatusEffectManager.Instance.ShouldAttackSelf(attacker))
            {
                // 攻击自己
                DamageResult selfDamageResult = DamageCalculator.CalculateDamage(attacker, attacker, skill);
                attacker.TakeDamage(selfDamageResult.damage);

                ShowDamagePopup(attacker, selfDamageResult.damage, selfDamageResult.elementMultiplier,
                               "混乱攻击自己!", GetDamageType(selfDamageResult));

                // 结束回合
                StartCoroutine(EndTurnAfterDelay(attacker));
                return;
            }

            // 计算伤害
            DamageResult damageResult = DamageCalculator.CalculateDamage(attacker, target, skill);

            // 应用伤害
            target.TakeDamage(damageResult.damage);

            // 显示伤害数字
            ShowDamagePopup(target, damageResult.damage, damageResult.elementMultiplier,
                           damageResult.GetDisplayText(), GetDamageType(damageResult));

            // 处理特殊效果
            ProcessSkillEffects(attacker, target, skill, damageResult.damage);

            // 应用状态效果
            if (skill.applyStatus != StatusCondition.None && skill.statusChance > 0)
            {
                // 检查状态施加概率
                if (Random.Range(0, 100) < skill.statusChance)
                {
                    ApplyStatusEffect(attacker, target, skill.applyStatus, skill.statusDuration, skill.statusDamageRate);
                }
            }

            // 清除状态效果
            if (skill.canClearStatus)
            {
                ClearStatusEffects(attacker, target, skill);
            }

            // 结束回合
            StartCoroutine(EndTurnAfterDelay(attacker));
        }

        // 执行属性技能
        private void ExecuteStatusSkill(PetEntity attacker, PetEntity target, SkillData skill)
        {
            // 检查命中（属性技能也需要命中判定）
            if (!DamageCalculator.CheckHit(skill.accuracy, attacker))
            {
                ShowStatusPopup(target, "Miss");
                StartCoroutine(EndTurnAfterDelay(attacker));
                return;
            }

            // 确定目标
            PetEntity actualTarget = skill.applyToTarget ? target : attacker;

            // 应用属性变化
            if (skill.HasStatModifiers())
            {
                foreach (var modifier in skill.statModifiers)
                {
                    actualTarget.ModifyStat(modifier.statType, modifier.value);
                    Debug.Log($"{actualTarget.petName} {modifier.ToString()}");
                }
            }

            // 应用状态效果
            if (skill.applyStatus != StatusCondition.None && skill.statusChance > 0)
            {
                // 检查状态施加概率
                if (Random.Range(0, 100) < skill.statusChance)
                {
                    ApplyStatusEffect(attacker, actualTarget, skill.applyStatus, skill.statusDuration, skill.statusDamageRate);
                }
            }

            // 清除状态效果
            if (skill.canClearStatus)
            {
                ClearStatusEffects(attacker, actualTarget, skill);
            }

            // 显示状态变化
            ShowStatusPopup(actualTarget, skill);

            // 结束回合
            StartCoroutine(EndTurnAfterDelay(attacker));
        }

        // 应用状态效果
        private void ApplyStatusEffect(PetEntity attacker, PetEntity target,
                                      StatusCondition condition, int customDuration, float damageRate)
        {
            var statusManager = StatusEffectManager.Instance;
            if (statusManager == null) return;

            // 确定持续时间：优先使用技能自定义的，否则使用默认的
            int duration = customDuration > 0 ? customDuration : StatusEffectConfig.GetDuration(condition);

            StatusEffect effect = new StatusEffect
            {
                condition = condition,
                remainingTurns = duration,
                sourcePetId = attacker?.GetInstanceID().ToString(),
                damageRate = damageRate
            };

            bool success = statusManager.AddStatusEffect(target, effect);

            if (success)
            {
                Debug.Log($"{target.petName} 被施加了 {condition} 状态，持续 {duration} 回合");
                ShowStatusPopup(target, $"{StatusEffectConfig.GetDisplayName(condition)}!");
            }
        }

        // 清除状态效果
        private void ClearStatusEffects(PetEntity attacker, PetEntity target, SkillData skill)
        {
            var statusManager = StatusEffectManager.Instance;
            if (statusManager == null) return;

            if (skill.clearAllStatus)
            {
                // 清除所有状态
                statusManager.ClearAllStatusEffects(target);
                Debug.Log($"{target.petName} 的所有状态被清除");
                ShowStatusClearPopup(target);
            }
            else if (skill.clearStatusType != StatusCondition.None)
            {
                // 清除特定类型状态
                statusManager.ClearStatusEffectsOfType(target, skill.clearStatusType);
                Debug.Log($"{target.petName} 的 {skill.clearStatusType} 状态被清除");
                ShowStatusClearPopup(target, skill.clearStatusType);
            }
        }

        // 处理技能特殊效果
        private void ProcessSkillEffects(PetEntity attacker, PetEntity target, SkillData skill, int damage)
        {
            switch (skill.effectType)
            {
                case SkillEffectType.Drain: // 吸血
                    int healAmount = Mathf.RoundToInt(damage * skill.effectValue);
                    if (healAmount > 0)
                    {
                        attacker.Heal(healAmount);
                        ShowHealPopup(attacker, healAmount); // 显示治疗数字
                    }
                    break;

                case SkillEffectType.PercentageDamage: // 百分比伤害
                    int percentDamage = Mathf.RoundToInt(target.CurrentHP * skill.effectValue);
                    target.TakeDamage(percentDamage);
                    ShowDamagePopup(target, percentDamage, 1.0f, "%DMG", DamageTextPop.DamageType.Normal);
                    break;

                case SkillEffectType.Heal: // 治疗
                    int heal = Mathf.RoundToInt(skill.power * skill.effectValue);
                    if (heal > 0)
                    {
                        target.Heal(heal);
                        ShowHealPopup(target, heal); // 显示治疗数字
                    }
                    break;
            }
        }

        // 获取伤害类型
        private DamageTextPop.DamageType GetDamageType(DamageResult result)
        {
            if (result.isCritical) return DamageTextPop.DamageType.Critical;

            switch (result.elementLabel)
            {
                case "SUPER": return DamageTextPop.DamageType.SuperEffective;
                case "WEAK":
                case "NVE": return DamageTextPop.DamageType.Weak;
                default: return DamageTextPop.DamageType.Normal;
            }
        }

        // 显示伤害数字
        private void ShowDamagePopup(PetEntity target, int damage, float multiplier, string prefix, DamageTextPop.DamageType type)
        {
            if (damageTextPrefab == null || damageCanvas == null)
            {
                Debug.LogWarning("伤害数字显示: 缺少预制体或画布引用");
                BattleEvents.TriggerDamageDealt(null, target, null, damage);
                return;
            }

            try
            {
                // 创建伤害数字
                GameObject damageTextObj = Instantiate(damageTextPrefab, damageCanvas);
                DamageTextPop damageText = damageTextObj.GetComponent<DamageTextPop>();

                if (damageText != null)
                {
                    // 获取目标位置
                    Vector3 targetPosition = Vector3.zero;

                    // 首先尝试获取UI位置
                    RectTransform targetRect = target.GetComponent<RectTransform>();
                    if (targetRect != null)
                    {
                        targetPosition = targetRect.position;
                    }
                    else
                    {
                        // 如果没有RectTransform，使用世界位置并转换为屏幕坐标
                        targetPosition = Camera.main.WorldToScreenPoint(target.transform.position);
                    }

                    // 添加随机偏移
                    Vector3 offset = new Vector3(
                        Random.Range(-30f, 30f),
                        damageNumberYOffset + Random.Range(-10f, 10f),
                        0
                    );

                    damageText.transform.position = targetPosition + offset;

                    // 显示伤害数字
                    damageText.ShowDamage(damage, type);
                    Debug.Log($"显示伤害数字: {target.petName} -{damage}");
                }

                BattleEvents.TriggerDamageDealt(null, target, null, damage);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"显示伤害数字时出错: {e.Message}");
            }

            // 输出到控制台
            string multiplierText = multiplier != 1.0f ? $" (x{multiplier})" : "";
            Debug.Log($"{target.petName} 受到 {damage} 点伤害 {multiplierText} ({prefix})");
        }

        // 显示治疗数字（修复版本）
        private void ShowHealPopup(PetEntity target, int healAmount)
        {
            if (target == null)
            {
                Debug.LogWarning("ShowHealPopup: 目标为空");
                return;
            }

            if (damageTextPrefab == null)
            {
                Debug.LogWarning("ShowHealPopup: 伤害数字预制体为空");
                // 尝试从Resources加载
                damageTextPrefab = Resources.Load<GameObject>("DamageTextPrefab");
                if (damageTextPrefab == null)
                {
                    Debug.LogError("ShowHealPopup: 无法加载伤害数字预制体");
                    return;
                }
            }

            if (damageCanvas == null)
            {
                // 尝试查找Damage Canvas
                GameObject canvasObj = GameObject.Find("Damage_Canvas");
                if (canvasObj != null)
                {
                    damageCanvas = canvasObj.GetComponent<RectTransform>();
                }

                if (damageCanvas == null)
                {
                    // 创建新的Canvas
                    canvasObj = new GameObject("Damage_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    damageCanvas = canvasObj.GetComponent<RectTransform>();
                    Canvas canvas = canvasObj.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 100; // 确保在最上层
                }
            }

            try
            {
                // 创建治疗数字
                GameObject healTextObj = Instantiate(damageTextPrefab, damageCanvas);
                DamageTextPop healText = healTextObj.GetComponent<DamageTextPop>();

                if (healText != null)
                {
                    // 获取目标位置
                    Vector3 targetPosition = Vector3.zero;

                    // 首先尝试获取UI位置
                    RectTransform targetRect = target.GetComponent<RectTransform>();
                    if (targetRect != null)
                    {
                        targetPosition = targetRect.position;
                    }
                    else
                    {
                        // 如果没有RectTransform，使用世界位置并转换为屏幕坐标
                        targetPosition = Camera.main.WorldToScreenPoint(target.transform.position);
                    }

                    // 添加随机偏移
                    Vector3 offset = new Vector3(
                        Random.Range(-30f, 30f),
                        damageNumberYOffset + Random.Range(20f, 40f), // 治疗数字比伤害数字更高
                        0
                    );

                    healText.transform.position = targetPosition + offset;

                    // 显示治疗数字 - 确保使用Heal类型
                    healText.ShowDamage(healAmount, DamageTextPop.DamageType.Heal);
                    Debug.Log($"显示治疗数字: {target.petName} +{healAmount}");
                }
                else
                {
                    Debug.LogWarning("治疗数字显示: DamageTextPop组件缺失");
                }

                // 可选：播放治疗特效
                if (healEffectPrefab != null)
                {
                    GameObject healEffect = Instantiate(healEffectPrefab, target.transform);
                    healEffect.transform.localPosition = Vector3.zero;
                    Destroy(healEffect, 1f);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"显示治疗数字时出错: {e.Message}\n{e.StackTrace}");
            }

            // 触发治疗事件
            BattleEvents.TriggerHealReceived(target, healAmount);
        }

        // 显示状态变化
        private void ShowStatusPopup(PetEntity target, SkillData skill)
        {
            string statusText = "状态变化";
            if (skill.HasStatModifiers())
            {
                statusText = "属性变化: ";
                foreach (var modifier in skill.statModifiers)
                {
                    statusText += modifier.ToString() + " ";
                }
            }

            Debug.Log($"{target.petName} {statusText}");
        }

        // 显示状态施加特效
        private void ShowStatusPopup(PetEntity target, string statusText)
        {
            Debug.Log($"{target.petName} {statusText}");
        }

        private void ShowStatusClearPopup(PetEntity target, StatusCondition? condition = null)
        {
            string text = condition.HasValue ?
                $"{StatusEffectConfig.GetDisplayName(condition.Value)} 清除!" :
                "状态清除!";
            Debug.Log($"{target.petName} {text}");
        }

        // 结束回合延迟
        private IEnumerator EndTurnAfterDelay(PetEntity attacker)
        {
            yield return new WaitForSeconds(statusEffectDelay);

            if (battleController != null)
            {
                battleController.EndTurn(attacker);
            }
        }
    }
}