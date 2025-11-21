using System;
using UnityEngine;

/// <summary>
/// 숫자 필드를 인스펙터에서 16진수(0xXXXXXXXX)로 표시하기 위한 Attribute.
/// int, long, uint 등에 붙여서 사용.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class HexViewAttribute : PropertyAttribute
{
}
