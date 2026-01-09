using System.Collections.Generic;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 管理所有宠物的异常状态
    /// </summary>
    public class StatusEffectManager : MonoBehaviour
    {
        private Dictionary<PetEntity, List<StatusEffect>> _petStatusEffects = new Dictionary<PetEntity, List<StatusEffect>>();

        public static StatusEffectManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 添加状态效果
        /// </summary>
        public bool AddStatusEffect(PetEntity target, StatusEffect newEffect)
        {
            if (target == null || target.isDead) return false;

            // 检查是否免疫该状态
            if (target.IsImmuneTo(newEffect.condition))
            {
                Debug.Log($"{target.petName} 免疫 {newEffect.condition} 状态");
                return false;
            }

            // 创建或获取该宠物的状态列表
            if (!_petStatusEffects.TryGetValue(target, out var effects))
            {
                effects = new List<StatusEffect>();
                _petStatusEffects[target] = effects;
            }

            // 检查是否已存在相同状态
            var existingEffect = effects.Find(e => e.condition == newEffect.condition);

            if (existingEffect != null)
            {
                if (StatusEffectConfig.IsStackable(newEffect.condition))
                {
                    // 对于寄生状态，我们只刷新持续时间，不增加层数
                    if (newEffect.condition == StatusCondition.Parasitic)
                    {
                        existingEffect.remainingTurns = Mathf.Max(existingEffect.remainingTurns, newEffect.remainingTurns);
                        Debug.Log($"{target.petName} 的 {newEffect.condition} 状态持续时间刷新为 {existingEffect.remainingTurns} 回合");
                    }
                    else
                    {
                        // 其他可叠加状态：增加层数
                        existingEffect.stackCount++;
                        existingEffect.remainingTurns = Mathf.Max(existingEffect.remainingTurns, newEffect.remainingTurns);
                        Debug.Log($"{target.petName} 的 {newEffect.condition} 状态叠加至 {existingEffect.stackCount} 层");
                    }

                    // 触发状态更新事件
                    BattleEvents.TriggerStatusEffectUpdated(target, existingEffect);
                    return true;
                }
                else
                {
                    // 不可叠加状态：刷新持续时间，但不触发添加事件
                    existingEffect.remainingTurns = Mathf.Max(existingEffect.remainingTurns, newEffect.remainingTurns);
                    Debug.Log($"{target.petName} 的 {newEffect.condition} 状态持续时间刷新为 {existingEffect.remainingTurns} 回合");

                    // 触发状态更新事件
                    BattleEvents.TriggerStatusEffectUpdated(target, existingEffect);
                    return true;
                }
            }
            else
            {
                // 检查新状态与已有状态是否可以共存
                bool canCoexist = true;
                foreach (var effect in effects)
                {
                    if (!StatusEffectConfig.CanCoexist(effect.condition, newEffect.condition))
                    {
                        canCoexist = false;
                        break;
                    }
                }

                if (!canCoexist)
                {
                    Debug.Log($"{target.petName} 不能同时拥有 {newEffect.condition} 状态与已有状态");
                    return false;
                }

                // 添加新状态
                effects.Add(newEffect);
                Debug.Log($"{target.petName} 获得 {newEffect.condition} 状态，持续 {newEffect.remainingTurns} 回合");

                // 触发状态添加事件
                BattleEvents.TriggerStatusEffectApplied(target, newEffect);
                return true;
            }
        }

        /// <summary>
        /// 移除状态效果
        /// </summary>
        public void RemoveStatusEffect(PetEntity target, StatusCondition condition)
        {
            if (_petStatusEffects.TryGetValue(target, out var effects))
            {
                var effect = effects.Find(e => e.condition == condition);
                if (effect != null)
                {
                    effects.Remove(effect);
                    Debug.Log($"{target.petName} 的 {condition} 状态被移除");

                    // 触发状态移除事件
                    BattleEvents.TriggerStatusEffectRemoved(target, condition);
                }

                if (effects.Count == 0)
                {
                    _petStatusEffects.Remove(target);
                }
            }
        }

        /// <summary>
        /// 清除所有状态效果
        /// </summary>
        public void ClearAllStatusEffects(PetEntity target)
        {
            if (_petStatusEffects.ContainsKey(target))
            {
                // 触发所有状态移除事件
                foreach (var effect in _petStatusEffects[target])
                {
                    BattleEvents.TriggerStatusEffectRemoved(target, effect.condition);
                }

                _petStatusEffects.Remove(target);
                Debug.Log($"{target.petName} 的所有状态效果被清除");
            }
        }

        /// <summary>
        /// 清除特定类型的状态效果
        /// </summary>
        public void ClearStatusEffectsOfType(PetEntity target, StatusCondition condition)
        {
            if (_petStatusEffects.TryGetValue(target, out var effects))
            {
                effects.RemoveAll(e => e.condition == condition);
                Debug.Log($"{target.petName} 的所有 {condition} 状态被清除");

                BattleEvents.TriggerStatusEffectRemoved(target, condition);

                if (effects.Count == 0)
                {
                    _petStatusEffects.Remove(target);
                }
            }
        }

        /// <summary>
        /// 获取宠物的状态效果
        /// </summary>
        public List<StatusEffect> GetStatusEffects(PetEntity target)
        {
            return _petStatusEffects.TryGetValue(target, out var effects) ? effects : new List<StatusEffect>();
        }

        /// <summary>
        /// 检查是否拥有特定状态
        /// </summary>
        public bool HasStatus(PetEntity target, StatusCondition condition)
        {
            if (_petStatusEffects.TryGetValue(target, out var effects))
            {
                return effects.Exists(e => e.condition == condition);
            }
            return false;
        }

        /// <summary>
        /// 获取状态层数
        /// </summary>
        public int GetStatusStackCount(PetEntity target, StatusCondition condition)
        {
            if (_petStatusEffects.TryGetValue(target, out var effects))
            {
                var effect = effects.Find(e => e.condition == condition);
                return effect?.stackCount ?? 0;
            }
            return 0;
        }

        /// <summary>
        /// 处理回合开始时的状态效果
        /// </summary>
        public void ProcessTurnStart(PetEntity pet)
        {
            if (!_petStatusEffects.TryGetValue(pet, out var effects)) return;

            // 检查所有状态，看是否可以行动
            foreach (var effect in effects)
            {
                if (!effect.CanTakeAction())
                {
                    Debug.Log($"{pet.petName} 因 {effect.condition} 状态无法行动");
                    BattleEvents.TriggerActionPrevented(pet, effect.condition);
                    return; // 只要有一个状态阻止行动，就无法行动
                }
            }
        }

        /// <summary>
        /// 处理回合结束时的状态效果（关键修复：改为public）
        /// </summary>
        public void ProcessTurnEnd(PetEntity pet)
        {
            if (!_petStatusEffects.TryGetValue(pet, out var effects)) return;

            List<StatusEffect> effectsToRemove = new List<StatusEffect>();

            foreach (var effect in effects)
            {
                // 计算持续伤害
                int damage = effect.CalculateTurnDamage(pet);
                if (damage > 0)
                {
                    pet.TakeDamage(damage);
                    Debug.Log($"{pet.petName} 因 {effect.condition} 状态受到 {damage} 点伤害");

                    // 触发状态伤害事件
                    BattleEvents.TriggerStatusDamageTick(pet, effect.condition, damage);

                    // 如果是寄生，治疗施放者
                    if (effect.condition == StatusCondition.Parasitic && !string.IsNullOrEmpty(effect.sourcePetId))
                    {
                        PetEntity sourcePet = FindPetById(effect.sourcePetId);
                        if (sourcePet != null && !sourcePet.isDead)
                        {
                            sourcePet.Heal(damage);
                            Debug.Log($"{sourcePet.petName} 通过寄生种子吸收 {damage} 点生命值");
                        }
                    }
                }

                // 回合结束处理
                effect.OnTurnEnd(pet);

                // 关键修复：无论是否移除，都触发更新事件
                BattleEvents.TriggerStatusEffectUpdated(pet, effect);

                // 检查状态是否结束
                if (effect.remainingTurns <= 0)
                {
                    effectsToRemove.Add(effect);
                }
            }

            // 移除已结束的状态
            foreach (var effect in effectsToRemove)
            {
                RemoveStatusEffect(pet, effect.condition);
            }
        }

        /// <summary>
        /// 获取状态对攻击力的乘数（所有状态叠加）
        /// </summary>
        public float GetStatusAttackMultiplier(PetEntity attacker)
        {
            if (!_petStatusEffects.TryGetValue(attacker, out var effects)) return 1.0f;

            float multiplier = 1.0f;
            foreach (var effect in effects)
            {
                multiplier *= effect.GetAttackMultiplier();
            }
            return multiplier;
        }

        /// <summary>
        /// 获取状态对命中率的乘数（所有状态叠加）
        /// </summary>
        public float GetStatusAccuracyMultiplier(PetEntity attacker)
        {
            if (!_petStatusEffects.TryGetValue(attacker, out var effects)) return 1.0f;

            float multiplier = 1.0f;
            foreach (var effect in effects)
            {
                multiplier *= effect.GetAccuracyMultiplier();
            }
            return multiplier;
        }

        /// <summary>
        /// 获取状态对治疗效果的乘数（所有状态叠加）
        /// </summary>
        public float GetStatusHealMultiplier(PetEntity target)
        {
            if (!_petStatusEffects.TryGetValue(target, out var effects)) return 1.0f;

            float multiplier = 1.0f;
            foreach (var effect in effects)
            {
                multiplier *= effect.GetHealMultiplier();
            }
            return multiplier;
        }

        /// <summary>
        /// 检查是否会攻击自己（混乱状态）
        /// </summary>
        public bool ShouldAttackSelf(PetEntity attacker)
        {
            if (!_petStatusEffects.TryGetValue(attacker, out var effects)) return false;

            foreach (var effect in effects)
            {
                if (effect.WillAttackSelf())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检查是否可以行动（考虑所有状态）
        /// </summary>
        public bool CanPetTakeAction(PetEntity pet)
        {
            if (!_petStatusEffects.TryGetValue(pet, out var effects)) return true;

            foreach (var effect in effects)
            {
                if (!effect.CanTakeAction())
                {
                    return false;
                }
            }
            return true;
        }

        private PetEntity FindPetById(string petId)
        {
            // 这里可以根据你的宠物ID查找逻辑来调整
            // 示例：根据实例ID查找
            var pets = FindObjectsOfType<PetEntity>();
            foreach (var pet in pets)
            {
                if (pet.GetInstanceID().ToString() == petId)
                    return pet;
            }
            return null;
        }

        /// <summary>
        /// 获取宠物的状态效果文本描述
        /// </summary>
        public string GetStatusEffectsText(PetEntity pet)
        {
            if (!_petStatusEffects.TryGetValue(pet, out var effects) || effects.Count == 0)
                return "无异常状态";

            string result = "";
            foreach (var effect in effects)
            {
                result += effect.GetDisplayText() + " ";
            }
            return result.Trim();
        }
    }
}