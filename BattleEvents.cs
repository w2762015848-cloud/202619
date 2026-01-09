using System;

namespace BattleSystem
{
    public static class BattleEvents
    {
        // 战斗事件
        public static event Action OnBattleStart;
        public static event Action OnBattleEnd;
        public static event Action OnTurnStart;
        public static event Action OnTurnEnd;

        // 宠物事件
        public static event Action<PetEntity, int, int> OnPetHPChanged; // 宠物，当前HP，最大HP
        public static event Action<PetEntity> OnPetDeath;
        public static event Action<PetEntity> OnPetRevive;
        public static event Action<PetEntity, BattleConstants.StatType, int> OnPetStatChanged; // 宠物，状态类型，变化值

        // 技能事件
        public static event Action<PetEntity, SkillData> OnSkillUsed;
        public static event Action<PetEntity, PetEntity, SkillData, int> OnDamageDealt; // 攻击者，目标，技能，伤害量
        public static event Action<PetEntity, int> OnHealReceived; // 目标，治疗量

        // UI事件
        public static event Action<PetEntity> OnPetUIUpdateNeeded;
        public static event Action<PetEntity> OnSkillButtonsUpdateNeeded;

        // 异常状态事件
        public static event Action<PetEntity, StatusEffect> OnStatusEffectApplied;
        public static event Action<PetEntity, StatusCondition> OnStatusEffectRemoved;
        public static event Action<PetEntity, StatusEffect> OnStatusEffectUpdated;
        public static event Action<PetEntity, StatusCondition> OnActionPrevented;
        public static event Action<PetEntity, StatusCondition, int> OnStatusDamageTick;

        // 触发方法
        public static void TriggerBattleStart() => OnBattleStart?.Invoke();
        public static void TriggerBattleEnd() => OnBattleEnd?.Invoke();
        public static void TriggerTurnStart() => OnTurnStart?.Invoke();
        public static void TriggerTurnEnd() => OnTurnEnd?.Invoke();

        public static void TriggerPetHPChanged(PetEntity pet, int currentHP, int maxHP) =>
            OnPetHPChanged?.Invoke(pet, currentHP, maxHP);

        public static void TriggerPetDeath(PetEntity pet) => OnPetDeath?.Invoke(pet);
        public static void TriggerPetRevive(PetEntity pet) => OnPetRevive?.Invoke(pet);

        public static void TriggerPetStatChanged(PetEntity pet, BattleConstants.StatType statType, int change) =>
            OnPetStatChanged?.Invoke(pet, statType, change);

        public static void TriggerSkillUsed(PetEntity attacker, SkillData skill) =>
            OnSkillUsed?.Invoke(attacker, skill);

        public static void TriggerDamageDealt(PetEntity attacker, PetEntity target, SkillData skill, int damage) =>
            OnDamageDealt?.Invoke(attacker, target, skill, damage);

        public static void TriggerHealReceived(PetEntity target, int amount) =>
            OnHealReceived?.Invoke(target, amount);

        public static void TriggerPetUIUpdateNeeded(PetEntity pet) =>
            OnPetUIUpdateNeeded?.Invoke(pet);

        public static void TriggerSkillButtonsUpdateNeeded(PetEntity pet) =>
            OnSkillButtonsUpdateNeeded?.Invoke(pet);

        public static void TriggerStatusEffectApplied(PetEntity pet, StatusEffect effect) =>
            OnStatusEffectApplied?.Invoke(pet, effect);

        public static void TriggerStatusEffectRemoved(PetEntity pet, StatusCondition condition) =>
            OnStatusEffectRemoved?.Invoke(pet, condition);

        public static void TriggerStatusEffectUpdated(PetEntity pet, StatusEffect effect) =>
            OnStatusEffectUpdated?.Invoke(pet, effect);

        public static void TriggerActionPrevented(PetEntity pet, StatusCondition condition) =>
            OnActionPrevented?.Invoke(pet, condition);

        public static void TriggerStatusDamageTick(PetEntity pet, StatusCondition condition, int damage) =>
            OnStatusDamageTick?.Invoke(pet, condition, damage);
    }
}