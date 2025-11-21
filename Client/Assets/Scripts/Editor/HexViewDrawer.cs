#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// [HexView]가 붙은 숫자 필드를 인스펙터에서 16진수로 보여주는 Drawer.
/// </summary>
[CustomPropertyDrawer(typeof(HexViewAttribute))]
public class HexViewDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        if (property.propertyType == SerializedPropertyType.Integer)
        {
            // Unity 내부에서 int/uint/long 모두 Integer로 들어옴
            long raw = property.longValue;
            uint asUint = unchecked((uint)raw);

            string hex = "0x" + asUint.ToString("X8");
            EditorGUI.LabelField(position, label.text, hex);
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "(HexView: Integer 타입만 지원)");
        }

        EditorGUI.EndProperty();
    }
}
#endif
