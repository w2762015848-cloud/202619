using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BattleSystem
{
    /// <summary>
    /// 自定义属性，用于在Inspector中显示枚举的下拉菜单
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class EnumPopupAttribute : PropertyAttribute
    {
        public Type enumType;

        public EnumPopupAttribute(Type enumType)
        {
            this.enumType = enumType;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 自定义PropertyDrawer，用于绘制EnumPopupAttribute标记的字段
    /// </summary>
    [CustomPropertyDrawer(typeof(EnumPopupAttribute))]
    public class EnumPopupDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnumPopupAttribute attr = attribute as EnumPopupAttribute;

            if (attr == null || !attr.enumType.IsEnum)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            if (property.propertyType == SerializedPropertyType.String)
            {
                // 当前值
                string currentValue = property.stringValue;

                // 获取所有枚举值和显示名称
                Array enumValues = Enum.GetValues(attr.enumType);
                string[] displayNames = new string[enumValues.Length];
                string[] enumNames = new string[enumValues.Length];

                for (int i = 0; i < enumValues.Length; i++)
                {
                    var value = enumValues.GetValue(i);
                    var fieldInfo = attr.enumType.GetField(value.ToString());
                    var inspectorNameAttr = fieldInfo.GetCustomAttributes(typeof(InspectorNameAttribute), false);

                    if (inspectorNameAttr.Length > 0)
                    {
                        displayNames[i] = ((InspectorNameAttribute)inspectorNameAttr[0]).displayName;
                    }
                    else
                    {
                        displayNames[i] = value.ToString();
                    }
                    enumNames[i] = value.ToString();
                }

                // 查找当前值的索引
                int currentIndex = Array.IndexOf(enumNames, currentValue);
                if (currentIndex < 0) currentIndex = 0;

                // 显示下拉菜单
                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUI.Popup(position, label.text, currentIndex, displayNames);

                if (EditorGUI.EndChangeCheck())
                {
                    property.stringValue = enumNames[newIndex];
                }
            }
            else if (property.propertyType == SerializedPropertyType.Enum)
            {
                // 如果是枚举类型，直接绘制
                EditorGUI.PropertyField(position, property, label);
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }
#endif
}