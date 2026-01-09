using System;
using UnityEngine;

namespace BattleSystem
{
    [CreateAssetMenu(fileName = "新技能", menuName = "战斗系统/技能")]
    public class SkillData : ScriptableObject
    {
        [Header("基本属性")]
        public string skillName = "新技能";

        [Tooltip("技能的元素属性")]
        [EnumPopup(typeof(ElementType))]
        public string element = BattleConstants.ELEMENT_NONE;

        [Tooltip("技能分类")]
        [EnumPopup(typeof(SkillCategory))]
        public string category = "攻击";

        [Tooltip("技能威力")]
        [Range(0, 200)] public int power = 100;

        [Tooltip("命中率 (0-100)")]
        [Range(0, 100)] public int accuracy = 100;

        [Header("PP设置")]
        [Tooltip("最大PP值")]
        [Range(1, 30)] public int maxPP = 10;
        [NonSerialized] public int currentPP;

        [Header("效果设置")]
        [Tooltip("是否对目标生效 (否则对自身生效)")]
        public bool applyToTarget = true;

        [Tooltip("技能效果类型")]
        [EnumPopup(typeof(SkillEffectType))]
        public SkillEffectType effectType = SkillEffectType.Damage;

        [Tooltip("效果数值")]
        public float effectValue = 1.0f;

        [Header("属性变化效果")]
        [Tooltip("属性变化列表")]
        public StatModifier[] statModifiers = new StatModifier[0];

        [Header("异常状态效果")]
        [Tooltip("技能施加的异常状态")]
        [EnumPopup(typeof(StatusCondition))]
        public StatusCondition applyStatus = StatusCondition.None;

        [Tooltip("状态施加概率 (0-100)")]
        [Range(0, 100)] public int statusChance = 100;

        [Tooltip("状态持续时间 (回合数，0表示使用默认值)")]
        [Range(0, 10)] public int statusDuration = 0;

        [Tooltip("状态伤害比例 (用于寄生、中毒等)")]
        [Range(0f, 1f)] public float statusDamageRate = 0.08f;

        [Header("清除状态效果")]
        [Tooltip("是否可以清除状态")]
        public bool canClearStatus = false;

        [Tooltip("清除所有状态")]
        public bool clearAllStatus = false;

        [Tooltip("清除特定类型状态")]
        [EnumPopup(typeof(StatusCondition))]
        public StatusCondition clearStatusType = StatusCondition.None;

        [Header("技能描述")]
        [TextArea(3, 5)] public string description = "技能描述";

        [Header("UI设置")]
        [Tooltip("技能图标")]
        public Sprite icon;

        [Tooltip("技能主题颜色")]
        public Color themeColor = Color.white;

        // 启用时重置PP
        private void OnEnable()
        {
            currentPP = maxPP;
        }

        // 检查是否有属性变化
        public bool HasStatModifiers()
        {
            return statModifiers != null && statModifiers.Length > 0;
        }

        // 检查是否为属性技能
        public bool IsStatusSkill()
        {
            if (string.IsNullOrEmpty(category)) return false;
            string cat = category.Trim();

            return cat == "属性" ||
                   cat == "辅助" ||
                   cat == "被动" ||
                   cat == BattleConstants.CATEGORY_STATUS ||
                   cat == BattleConstants.CATEGORY_SUPPORT;
        }

        // 重置PP
        public void ResetPP()
        {
            currentPP = maxPP;
        }

        // 克隆
        public SkillData Clone()
        {
            SkillData clone = CreateInstance<SkillData>();

            clone.skillName = this.skillName;
            clone.element = this.element;
            clone.category = this.category;
            clone.power = this.power;
            clone.accuracy = this.accuracy;
            clone.maxPP = this.maxPP;
            clone.currentPP = this.currentPP;
            clone.applyToTarget = this.applyToTarget;
            clone.effectType = this.effectType;
            clone.effectValue = this.effectValue;

            if (this.statModifiers != null)
            {
                clone.statModifiers = new StatModifier[this.statModifiers.Length];
                Array.Copy(this.statModifiers, clone.statModifiers, this.statModifiers.Length);
            }

            clone.applyStatus = this.applyStatus;
            clone.statusChance = this.statusChance;
            clone.statusDuration = this.statusDuration;
            clone.statusDamageRate = this.statusDamageRate;
            clone.canClearStatus = this.canClearStatus;
            clone.clearAllStatus = this.clearAllStatus;
            clone.clearStatusType = this.clearStatusType;

            clone.description = this.description;
            clone.icon = this.icon;
            clone.themeColor = this.themeColor;

            return clone;
        }
    }

    // 枚举定义（放在同一个文件中确保顺序正确）
    public enum SkillEffectType
    {
        [InspectorName("无效果")]
        None = 0,
        [InspectorName("造成伤害")]
        Damage = 1,
        [InspectorName("治疗")]
        Heal = 2,
        [InspectorName("属性变化")]
        StatModifier = 3,
        [InspectorName("吸血")]
        Drain = 4,
        [InspectorName("百分比伤害")]
        PercentageDamage = 5,
        [InspectorName("固定伤害")]
        FixedDamage = 6,
        [InspectorName("必定暴击")]
        GuaranteedCrit = 7,
        [InspectorName("施加状态")]
        ApplyStatus = 8,
        [InspectorName("清除状态")]
        ClearStatus = 9
    }

    public enum SkillCategory
    {
        [InspectorName("攻击")]
        Attack = 0,
        [InspectorName("特攻")]
        Special = 1,
        [InspectorName("属性")]
        Status = 2,
        [InspectorName("辅助")]
        Support = 3,
        [InspectorName("被动")]
        Passive = 4
    }

    public enum ElementType
    {
        [InspectorName("无")]
        None = 0,
        [InspectorName("火")]
        Fire = 1,
        [InspectorName("水")]
        Water = 2,
        [InspectorName("草")]
        Grass = 3,
        [InspectorName("雷")]
        Electric = 4,
        [InspectorName("岩")]
        Rock = 5,
        [InspectorName("风")]
        Wind = 6,
        [InspectorName("毒")]
        Poison = 7,
        [InspectorName("光")]
        Light = 8,
        [InspectorName("暗")]
        Dark = 9,
        [InspectorName("圣灵")]
        Holy = 10,
        [InspectorName("机械")]
        Mech = 11
    }
}