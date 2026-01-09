using System;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 异常状态类型
    /// </summary>
    public enum StatusCondition
    {
        [InspectorName("无")]
        None = 0,

        [InspectorName("烧伤")]
        Burn = 1,

        [InspectorName("冰冻")]
        Freeze = 2,

        [InspectorName("麻痹")]
        Paralyze = 3,

        [InspectorName("中毒")]
        Poison = 4,

        [InspectorName("失明")]
        Blind = 5,

        [InspectorName("混乱")]
        Confusion = 6,

        [InspectorName("寄生")]
        Parasitic = 7,

        [InspectorName("眩晕")]
        Stun = 8
    }

    /// <summary>
    /// 异常状态配置
    /// </summary>
    public static class StatusEffectConfig
    {
        // 状态默认持续时间（回合数），0表示永久
        public static int GetDuration(StatusCondition condition)
        {
            return condition switch
            {
                StatusCondition.Burn => 4,
                StatusCondition.Freeze => 3,
                StatusCondition.Paralyze => 4,
                StatusCondition.Poison => 5,
                StatusCondition.Blind => 3,
                StatusCondition.Confusion => 3,
                StatusCondition.Parasitic => 5,
                StatusCondition.Stun => 2,
                _ => 0
            };
        }

        // 状态是否可以叠加（同一状态多个层数）
        public static bool IsStackable(StatusCondition condition)
        {
            return condition == StatusCondition.Parasitic; // 寄生可以叠加层数
        }

        // 状态是否可以共存（不同状态同时存在）
        public static bool CanCoexist(StatusCondition condition1, StatusCondition condition2)
        {
            // 所有不同状态都可以共存
            if (condition1 != condition2) return true;

            // 相同状态：只有可叠加的状态可以共存
            return IsStackable(condition1);
        }

        // 状态显示名称
        public static string GetDisplayName(StatusCondition condition)
        {
            return condition switch
            {
                StatusCondition.Burn => "烧伤",
                StatusCondition.Freeze => "冰冻",
                StatusCondition.Paralyze => "麻痹",
                StatusCondition.Poison => "中毒",
                StatusCondition.Blind => "失明",
                StatusCondition.Confusion => "混乱",
                StatusCondition.Parasitic => "寄生",
                StatusCondition.Stun => "眩晕",
                _ => "无"
            };
        }

        // 状态是否会造成持续伤害
        public static bool IsDamageOverTime(StatusCondition condition)
        {
            return condition == StatusCondition.Burn ||
                   condition == StatusCondition.Poison ||
                   condition == StatusCondition.Parasitic;
        }

        // 获取状态默认伤害比例
        public static float GetDefaultDamageRate(StatusCondition condition)
        {
            return condition switch
            {
                StatusCondition.Burn => 0.1f,    // 10%最大生命值
                StatusCondition.Poison => 0.1f,  // 10%最大生命值
                StatusCondition.Parasitic => 0.08f, // 8%最大生命值
                _ => 0f
            };
        }
    }

    /// <summary>
    /// 异常状态实例
    /// </summary>
    [Serializable]
    public class StatusEffect
    {
        public StatusCondition condition;
        public int remainingTurns;      // 剩余回合数
        public int stackCount = 1;      // 层数（用于寄生）
        public string sourcePetId;      // 施加者ID（用于寄生）
        public float damageRate = 0f;   // 伤害比例（可自定义）

        // 计算回合伤害
        public int CalculateTurnDamage(PetEntity target)
        {
            if (!StatusEffectConfig.IsDamageOverTime(condition))
                return 0;

            // 使用自定义伤害比例或默认值
            float rate = damageRate > 0 ? damageRate : StatusEffectConfig.GetDefaultDamageRate(condition);

            // 寄生伤害受层数影响
            if (condition == StatusCondition.Parasitic)
            {
                return Mathf.Max(1, (int)(target.MaxHP * rate * stackCount));
            }

            return Mathf.Max(1, (int)(target.MaxHP * rate));
        }

        // 获取命中率乘数
        public float GetAccuracyMultiplier()
        {
            return condition == StatusCondition.Blind ? 0.5f : 1.0f; // 失明命中率降低50%
        }

        // 获取攻击力乘数
        public float GetAttackMultiplier()
        {
            return condition == StatusCondition.Burn ? 0.9f : 1.0f; // 烧伤攻击力降低10%
        }

        // 检查是否可以行动
        public bool CanTakeAction()
        {
            switch (condition)
            {
                case StatusCondition.Freeze:
                case StatusCondition.Stun:
                    return false;

                case StatusCondition.Paralyze:
                    return UnityEngine.Random.value > 0.25f; // 25%概率无法行动

                default:
                    return true;
            }
        }

        // 检查是否会攻击自己
        public bool WillAttackSelf()
        {
            if (condition == StatusCondition.Confusion)
            {
                return UnityEngine.Random.value < 0.4f; // 40%概率攻击自己
            }
            return false;
        }

        // 获取治疗乘数
        public float GetHealMultiplier()
        {
            return condition == StatusCondition.Poison ? 0.5f : 1.0f; // 中毒治疗效果减半
        }

        // 回合结束处理
        public void OnTurnEnd(PetEntity target)
        {
            if (remainingTurns > 0)
                remainingTurns--;
        }

        // 获取显示文本
        public string GetDisplayText()
        {
            string name = StatusEffectConfig.GetDisplayName(condition);
            if (stackCount > 1)
            {
                return $"{name}({stackCount})";
            }
            return name;
        }

        // 检查状态是否结束
        public bool IsExpired()
        {
            return remainingTurns <= 0;
        }
    }
}