using UnityEngine;

namespace BattleSystem
{
    public static class BattleConstants
    {
        // 技能分类
        public const string CATEGORY_PHYSICAL = "攻击";
        public const string CATEGORY_SPECIAL = "特攻";
        public const string CATEGORY_STATUS = "属性";
        public const string CATEGORY_SUPPORT = "辅助";

        // 元素类型
        public const string ELEMENT_NONE = "无";
        public const string ELEMENT_FIRE = "火";
        public const string ELEMENT_WATER = "水";
        public const string ELEMENT_GRASS = "草";
        public const string ELEMENT_ELECTRIC = "雷";
        public const string ELEMENT_ROCK = "岩";
        public const string ELEMENT_WIND = "风";
        public const string ELEMENT_POISON = "毒";
        public const string ELEMENT_LIGHT = "光";
        public const string ELEMENT_DARK = "暗";
        public const string ELEMENT_HOLY = "圣灵";
        public const string ELEMENT_MECH = "机械";

        // 能力类型
        public enum StatType
        {
            [InspectorName("无")]
            None = 0,

            [InspectorName("攻击")]
            Attack = 1,

            [InspectorName("特攻")]
            MagicAttack = 2,

            [InspectorName("防御")]
            Defense = 3,

            [InspectorName("特防")]
            MagicDefense = 4,

            [InspectorName("速度")]
            Speed = 5,

            [InspectorName("暴击")]
            Critical = 6
        }

        // 最大最小能力等级
        public const int MIN_STAT_LEVEL = -6;
        public const int MAX_STAT_LEVEL = 6;
        public const float STAT_LEVEL_MULTIPLIER = 0.1f; // 每级10%

        // 颜色定义
        public static readonly Color PHYSICAL_COLOR = new Color(1f, 0.45f, 0f);
        public static readonly Color SPECIAL_COLOR = new Color(0.2f, 0.6f, 1f);
        public static readonly Color STATUS_COLOR = new Color(0.3f, 0.8f, 0.3f);
        public static readonly Color TEXT_DEFAULT_COLOR = Color.black;

        // UI设置
        public const float HP_ANIMATION_DURATION = 0.5f;
        public const float ENEMY_TURN_DELAY = 1.5f;

        // 元素克制表
        public static readonly (string attacker, string defender, float multiplier, string label)[] ElementAdvantages =
        {
            ("雷", "岩", 2.0f, "SUPER"),
            ("岩", "风", 2.0f, "SUPER"),
            ("风", "毒", 2.0f, "SUPER"),
            ("毒", "水", 2.0f, "SUPER"),
            ("水", "火", 2.0f, "SUPER"),
            ("火", "草", 2.0f, "SUPER"),
            ("草", "光", 2.0f, "SUPER"),
            ("光", "暗", 2.0f, "SUPER"),
            ("暗", "圣灵", 2.0f, "SUPER"),
            ("圣灵", "机械", 2.0f, "SUPER"),
            ("机械", "雷", 2.0f, "SUPER")
        };

        // 普通克制
        public static readonly (string attacker, string[] defenders, float multiplier, string label)[] NormalAdvantages =
        {
            ("雷", new[]{"风", "水", "圣灵"}, 1.5f, "EFF"),
            ("岩", new[]{"毒", "火", "光"}, 1.5f, "EFF"),
            ("风", new[]{"水", "光", "暗"}, 1.5f, "EFF"),
            ("毒", new[]{"草", "暗", "机械"}, 1.5f, "EFF"),
            ("水", new[]{"草", "圣灵", "岩"}, 1.5f, "EFF"),
            ("火", new[]{"光", "毒", "风"}, 1.5f, "EFF"),
            ("草", new[]{"暗", "风", "岩"}, 1.5f, "EFF"),
            ("光", new[]{"圣灵", "雷", "水"}, 1.5f, "EFF"),
            ("暗", new[]{"机械", "岩", "火"}, 1.5f, "EFF"),
            ("圣灵", new[]{"岩", "风", "毒"}, 1.5f, "EFF"),
            ("机械", new[]{"岩", "水", "草"}, 1.5f, "EFF")
        };
    }
}