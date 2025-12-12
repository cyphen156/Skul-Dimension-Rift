using Assets.Scripts.Data;

public static class DomainKey
{
    // DomainKey 32bit 레이아웃 (상위 → 하위):
    // [ 31 … 28 ] Domain (4bit)
    // [ 27 … 24 ] Grade (4bit)
    // [ 23 … 20 ] Role (4bit)
    // [ 19 … 12 ] Class (8bit)
    // [ 11 … 0 ] Instance (12bit)

    // 비트 위치 (상위 → 하위)
    private const int DomainShift = 28; // 4bit
    private const int GradeShift = 24; // 4bit
    private const int RoleShift = 20; // 4bit
    private const int ClassShift = 12; // 8bit

    // 마스크
    private const uint DomainMask = 0xFu;    // 4bit
    private const uint GradeMask = 0xFu;    // 4bit
    private const uint RoleMask = 0xFu;    // 4bit
    private const uint ClassMask = 0xFFu;   // 8bit
    private const uint InstanceMask = 0x0FFFu; // 12bit

    public static uint Make(
        Domain domain,
        byte grade,
        byte role,
        byte clazz,
        ushort instance // 0~4095
    )
    {
        uint value = 0u;

        value |= ((uint)domain & DomainMask) << DomainShift;
        value |= ((uint)grade & GradeMask) << GradeShift;
        value |= ((uint)role & RoleMask) << RoleShift;
        value |= ((uint)clazz & ClassMask) << ClassShift;
        value |= ((uint)instance & InstanceMask);

        return value;
    }

    public static Domain GetDomain(uint key)
    {
        uint domain = (key >> DomainShift) & DomainMask;
        return (Domain)domain;
    }

    public static byte GetGrade(uint key)
    {
        uint grade = (key >> GradeShift) & GradeMask;
        return (byte)grade;
    }

    public static byte GetRole(uint key)
    {
        uint role = (key >> RoleShift) & RoleMask;
        return (byte)role;
    }

    public static byte GetClass(uint key)
    {
        uint clazz = (key >> ClassShift) & ClassMask;
        return (byte)clazz;
    }

    public static ushort GetInstance(uint key)
    {
        uint instance = key & InstanceMask;
        return (ushort)instance;
    }

    /// <summary>
    /// Instance(하위 12bit)를 제거한 "정적 ID" 추출.
    /// 테이블 키, 리소스 매핑, DB 경로 등에 사용.
    /// </summary>
    public static uint GetStaticId(uint key)
    {
        return key & ~InstanceMask;
    }

    /// <summary>
    /// StaticId + Instance 를 다시 합치는 헬퍼.
    /// </summary>
    public static uint WithInstance(uint staticId, ushort instance)
    {
        uint value = staticId & ~InstanceMask;
        value |= (uint)instance & InstanceMask;
        return value;
    }

    public static string ToHex8(uint key)
    {
        return key.ToString("X8");
    }
}