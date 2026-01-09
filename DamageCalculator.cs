using UnityEngine;

namespace BattleSystem
{
    public class DamageResult
    {
        public int damage;
        public float elementMultiplier;
        public bool isCritical;
        public string elementLabel;

        public string GetDisplayText()
        {
            string display = "";
            if (isCritical) display += "暴击! ";
            display += elementLabel;
            return display.Trim();
        }
    }

    public static class DamageCalculator
    {
        // 计算伤害
        public static DamageResult CalculateDamage(PetEntity attacker, PetEntity defender, SkillData skill)
        {
            // 固定伤害
            if (skill.effectType == SkillEffectType.FixedDamage)
            {
                return new DamageResult
                {
                    damage = Mathf.RoundToInt(skill.effectValue),
                    elementMultiplier = 1.0f,
                    isCritical = false,
                    elementLabel = "固定伤害"
                };
            }

            // 支持中文分类判断
            bool isPhysical = IsPhysicalSkill(skill.category);

            float attack = attacker.GetAttack(isPhysical);
            float defense = defender.GetDefense(isPhysical);

            // 防止除零
            if (defense <= 0) defense = 1;
            if (attack <= 0) attack = 1;

            // 添加：状态对攻击力的修正
            float statusAttackMultiplier = 1.0f;
            var statusManager = StatusEffectManager.Instance;
            if (statusManager != null)
            {
                statusAttackMultiplier = statusManager.GetStatusAttackMultiplier(attacker);
            }

            // 属性克制
            ElementEffect elementEffect = CalculateElementMultiplier(skill.element, defender.element);

            // 暴击
            float critChance = 0.05f + attacker.criticalLevel * 0.05f;
            bool isCrit = (skill.effectType == SkillEffectType.GuaranteedCrit) ||
                         (Random.value < critChance);
            float critMultiplier = isCrit ? 1.5f : 1.0f;

            // 被动技能
            float passiveMultiplier = CalculatePassiveMultiplier(attacker, skill);

            // 基础伤害公式（添加状态修正）
            float baseDamage = (attack * skill.power * statusAttackMultiplier) / defense;

            // 最终伤害计算
            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(
                baseDamage * elementEffect.multiplier * critMultiplier *
                Random.Range(0.85f, 1.15f) * passiveMultiplier
            ));

            return new DamageResult
            {
                damage = finalDamage,
                elementMultiplier = elementEffect.multiplier,
                isCritical = isCrit,
                elementLabel = elementEffect.label
            };
        }

        // 检查是否为物理技能 - 支持中文分类
        private static bool IsPhysicalSkill(string category)
        {
            if (string.IsNullOrEmpty(category)) return true;

            string cat = category.Trim();

            // 支持中文分类名称
            return cat.Contains("攻击") ||
                   cat == "攻击" ||
                   cat == BattleConstants.CATEGORY_PHYSICAL ||
                   cat.ToLower() == "physical";
        }

        // 修改命中检查方法，添加状态影响
        public static bool CheckHit(int accuracy, PetEntity attacker = null)
        {
            float hitRate = accuracy <= 0 ? 100f : accuracy;

            // 状态对命中率的影响
            if (attacker != null)
            {
                var statusManager = StatusEffectManager.Instance;
                if (statusManager != null)
                {
                    float accuracyMultiplier = statusManager.GetStatusAccuracyMultiplier(attacker);
                    hitRate *= accuracyMultiplier;
                }
            }

            bool hit = Random.Range(0, 100) <= hitRate;
            return hit;
        }

        // 计算属性克制
        private static ElementEffect CalculateElementMultiplier(string attackerElement, string defenderElement)
        {
            // 默认值
            ElementEffect result = new ElementEffect
            {
                multiplier = 1.0f,
                label = ""
            };

            if (string.IsNullOrEmpty(attackerElement) || string.IsNullOrEmpty(defenderElement) ||
                attackerElement == BattleConstants.ELEMENT_NONE || defenderElement == BattleConstants.ELEMENT_NONE)
                return result;

            // 检查绝对克制
            foreach (var advantage in BattleConstants.ElementAdvantages)
            {
                if (advantage.attacker == attackerElement && advantage.defender == defenderElement)
                {
                    result.multiplier = advantage.multiplier;
                    result.label = advantage.label;
                    return result;
                }

                // 检查被绝对克制
                if (advantage.attacker == defenderElement && advantage.defender == attackerElement)
                {
                    result.multiplier = 1.0f / advantage.multiplier;
                    result.label = "WEAK";
                    return result;
                }
            }

            // 检查普通克制
            foreach (var advantage in BattleConstants.NormalAdvantages)
            {
                if (advantage.attacker == attackerElement)
                {
                    foreach (var defender in advantage.defenders)
                    {
                        if (defender == defenderElement)
                        {
                            result.multiplier = advantage.multiplier;
                            result.label = advantage.label;
                            return result;
                        }
                    }
                }

                // 检查被普通克制
                if (advantage.attacker == defenderElement)
                {
                    foreach (var defender in advantage.defenders)
                    {
                        if (defender == attackerElement)
                        {
                            result.multiplier = 1.0f / advantage.multiplier;
                            result.label = "NVE";
                            return result;
                        }
                    }
                }
            }

            return result;
        }

        // 计算被动技能加成
        private static float CalculatePassiveMultiplier(PetEntity attacker, SkillData skill)
        {
            if (attacker.passiveSkill == null) return 1.0f;

            // 示例：枫之血脉 - HP低于50%时伤害提升30%
            if (attacker.passiveSkill.skillName == "枫之血脉")
            {
                float hpPercent = (float)attacker.CurrentHP / attacker.MaxHP;
                if (hpPercent <= 0.5f)
                {
                    return 1.3f;
                }
            }

            return 1.0f;
        }

        // 内部结构
        private struct ElementEffect
        {
            public float multiplier;
            public string label;
        }
    }
}