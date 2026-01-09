using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleSystem
{
    [RequireComponent(typeof(PetUI))]
    public class PetEntity : MonoBehaviour
    {
        [Header("基本信息")]
        public string petName = "宠物";
        public string element = BattleConstants.ELEMENT_NONE;

        [Header("基础属性")]
        [SerializeField] private int _maxHP = 100;
        [SerializeField] private int _currentHP = 100;
        public int attack = 80;
        public int magicAttack = 90;
        public int defense = 70;
        public int magicDefense = 80;
        public int speed = 100;

        [Header("能力等级")]
        [Range(BattleConstants.MIN_STAT_LEVEL, BattleConstants.MAX_STAT_LEVEL)]
        public int attackLevel = 0;
        [Range(BattleConstants.MIN_STAT_LEVEL, BattleConstants.MAX_STAT_LEVEL)]
        public int magicAttackLevel = 0;
        [Range(BattleConstants.MIN_STAT_LEVEL, BattleConstants.MAX_STAT_LEVEL)]
        public int defenseLevel = 0;
        [Range(BattleConstants.MIN_STAT_LEVEL, BattleConstants.MAX_STAT_LEVEL)]
        public int magicDefenseLevel = 0;
        [Range(BattleConstants.MIN_STAT_LEVEL, BattleConstants.MAX_STAT_LEVEL)]
        public int speedLevel = 0;
        [Range(BattleConstants.MIN_STAT_LEVEL, BattleConstants.MAX_STAT_LEVEL)]
        public int criticalLevel = 0;

        [Header("技能")]
        public List<SkillData> skills = new List<SkillData>();
        public SkillData passiveSkill;

        [Header("状态")]
        public bool isDead = false;
        public bool isStunned = false;

        [Header("状态免疫")]
        public List<StatusCondition> immunities = new List<StatusCondition>();

        // 属性访问器
        public int MaxHP => _maxHP;
        public int CurrentHP => _currentHP;

        private PetUI _petUI;

        // 获取实际属性值（包含等级修正）
        public float GetAttack(bool isPhysical)
        {
            float baseValue = isPhysical ? attack : magicAttack;
            float levelMultiplier = 1 + (isPhysical ? attackLevel : magicAttackLevel) * BattleConstants.STAT_LEVEL_MULTIPLIER;
            return baseValue * levelMultiplier;
        }

        public float GetDefense(bool isPhysical)
        {
            float baseValue = isPhysical ? defense : magicDefense;
            float levelMultiplier = 1 + (isPhysical ? defenseLevel : magicDefenseLevel) * BattleConstants.STAT_LEVEL_MULTIPLIER;
            return baseValue * levelMultiplier;
        }

        // 受到伤害
        public void TakeDamage(int damage)
        {
            if (isDead) return;

            int oldHP = _currentHP;
            _currentHP = Mathf.Clamp(_currentHP - damage, 0, _maxHP);

            if (_currentHP <= 0)
            {
                _currentHP = 0;
                isDead = true;
                BattleEvents.TriggerPetDeath(this);
            }

            BattleEvents.TriggerPetHPChanged(this, _currentHP, _maxHP);
            BattleEvents.TriggerPetUIUpdateNeeded(this);
        }

        // 治疗
        public void Heal(int amount)
        {
            if (isDead) return;

            // 应用状态对治疗的修正
            float healMultiplier = 1.0f;
            var statusManager = StatusEffectManager.Instance;
            if (statusManager != null)
            {
                healMultiplier = statusManager.GetStatusHealMultiplier(this);
            }

            int finalHeal = Mathf.RoundToInt(amount * healMultiplier);
            int oldHP = _currentHP;
            _currentHP = Mathf.Clamp(_currentHP + finalHeal, 0, _maxHP);

            BattleEvents.TriggerPetHPChanged(this, _currentHP, _maxHP);
            BattleEvents.TriggerHealReceived(this, finalHeal);
            BattleEvents.TriggerPetUIUpdateNeeded(this);

            if (healMultiplier < 1.0f)
            {
                Debug.Log($"{petName} 因异常状态治疗效果减半，实际治疗{finalHeal}点");
            }
        }

        // 修改能力等级
        public void ModifyStat(BattleConstants.StatType statType, int change)
        {
            change = Mathf.Clamp(change, BattleConstants.MIN_STAT_LEVEL, BattleConstants.MAX_STAT_LEVEL);

            switch (statType)
            {
                case BattleConstants.StatType.Attack:
                    attackLevel = Mathf.Clamp(attackLevel + change, BattleConstants.MIN_STAT_LEVEL, BattleConstants.MAX_STAT_LEVEL);
                    break;
                case BattleConstants.StatType.MagicAttack:
                    magicAttackLevel = Mathf.Clamp(magicAttackLevel + change, BattleConstants.MIN_STAT_LEVEL, BattleConstants.MAX_STAT_LEVEL);
                    break;
                case BattleConstants.StatType.Defense:
                    defenseLevel = Mathf.Clamp(defenseLevel + change, BattleConstants.MIN_STAT_LEVEL, BattleConstants.MAX_STAT_LEVEL);
                    break;
                case BattleConstants.StatType.MagicDefense:
                    magicDefenseLevel = Mathf.Clamp(magicDefenseLevel + change, BattleConstants.MIN_STAT_LEVEL, BattleConstants.MAX_STAT_LEVEL);
                    break;
                case BattleConstants.StatType.Speed:
                    speedLevel = Mathf.Clamp(speedLevel + change, BattleConstants.MIN_STAT_LEVEL, BattleConstants.MAX_STAT_LEVEL);
                    break;
                case BattleConstants.StatType.Critical:
                    criticalLevel = Mathf.Clamp(criticalLevel + change, BattleConstants.MIN_STAT_LEVEL, BattleConstants.MAX_STAT_LEVEL);
                    break;
            }

            BattleEvents.TriggerPetStatChanged(this, statType, change);
        }

        // 重置状态
        public void ResetState()
        {
            _currentHP = _maxHP;
            isDead = false;
            isStunned = false;

            attackLevel = 0;
            magicAttackLevel = 0;
            defenseLevel = 0;
            magicDefenseLevel = 0;
            speedLevel = 0;
            criticalLevel = 0;

            // 重置技能PP
            foreach (var skill in skills)
            {
                if (skill != null)
                    skill.ResetPP();
            }

            // 清除状态效果
            var statusManager = StatusEffectManager.Instance;
            if (statusManager != null)
            {
                statusManager.ClearAllStatusEffects(this);
            }

            BattleEvents.TriggerPetUIUpdateNeeded(this);
        }

        // 设置HP（用于初始化）
        public void SetHP(int hp, int maxHP)
        {
            _currentHP = Mathf.Clamp(hp, 0, maxHP);
            _maxHP = Mathf.Max(1, maxHP);
            BattleEvents.TriggerPetHPChanged(this, _currentHP, _maxHP);
        }

        // 检查是否免疫某种状态
        public bool IsImmuneTo(StatusCondition condition)
        {
            // 检查元素免疫
            switch (condition)
            {
                case StatusCondition.Burn:
                    return element == BattleConstants.ELEMENT_FIRE;

                case StatusCondition.Freeze:
                    return element == BattleConstants.ELEMENT_WATER;

                case StatusCondition.Paralyze:
                    return element == BattleConstants.ELEMENT_ELECTRIC;

                case StatusCondition.Poison:
                    return element == BattleConstants.ELEMENT_POISON ||
                           element == BattleConstants.ELEMENT_MECH;
            }

            // 检查自定义免疫列表
            return immunities.Contains(condition);
        }

        void Awake()
        {
            _petUI = GetComponent<PetUI>();
            if (_petUI == null)
            {
                _petUI = gameObject.AddComponent<PetUI>();
            }
        }

        void Start()
        {
            BattleEvents.TriggerPetHPChanged(this, _currentHP, _maxHP);
        }
    }
}