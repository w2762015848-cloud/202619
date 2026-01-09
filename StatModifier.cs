using System;

namespace BattleSystem
{
    [Serializable]
    public struct StatModifier
    {
        public BattleConstants.StatType statType;
        public int value;
        public bool isPercentage;

        public StatModifier(BattleConstants.StatType type, int val, bool isPercent = false)
        {
            statType = type;
            value = val;
            isPercentage = isPercent;
        }

        public override string ToString()
        {
            string statName = GetStatName(statType);
            string sign = value > 0 ? "+" : "";
            return $"{statName}{sign}{value}{(isPercentage ? "%" : "")}";
        }

        private static string GetStatName(BattleConstants.StatType type)
        {
            return type switch
            {
                BattleConstants.StatType.Attack => "¹¥»÷",
                BattleConstants.StatType.MagicAttack => "ÌØ¹¥",
                BattleConstants.StatType.Defense => "·ÀÓù",
                BattleConstants.StatType.MagicDefense => "ÌØ·À",
                BattleConstants.StatType.Speed => "ËÙ¶È",
                BattleConstants.StatType.Critical => "±©»÷",
                _ => "Î´Öª"
            };
        }
    }
}