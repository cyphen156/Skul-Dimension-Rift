using Assets.Scripts.Data;

public static class ObjectKey
{
    // 비트 위치
    private const int DomainShift = 28; // 4bit
    private const int RoleShift = 20; // 8bit
    private const int GradeShift = 16; // 4bit
    private const int ClassShift = 12; // 4bit

    // 마스크
    private const uint DomainMask = 0xFu;
    private const uint RoleMask = 0xFFu;
    private const uint GradeMask = 0xFu;
    private const uint ClassMask = 0xFu;
    private const uint InstanceMask = 0x0FFFu;

    // 메인 생성 함수
    public static uint Make(
        ObjectDomain domain,
        byte role,
        byte grade,
        byte clazz,
        ushort instance // 0~4095
    )
    {
        uint value = 0u;

        value |= ((uint)domain & DomainMask) << DomainShift;
        value |= ((uint)role & RoleMask) << RoleShift;
        value |= ((uint)grade & GradeMask) << GradeShift;
        value |= ((uint)clazz & ClassMask) << ClassShift;
        value |= ((uint)instance & InstanceMask);

        return value;
    }

    // 정적/도메인 정보 읽기
    public static ObjectDomain GetDomain(uint key)
    {
        uint domain = (key >> DomainShift) & DomainMask;
        return (ObjectDomain)domain;
    }

    public static byte GetRole(uint key)
    {
        uint role = (key >> RoleShift) & RoleMask;
        return (byte)role;
    }

    public static byte GetGrade(uint key)
    {
        uint grade = (key >> GradeShift) & GradeMask;
        return (byte)grade;
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

    // StaticId (Instance 제거한 상위 20비트) 추출
    public static uint GetStaticId(uint key)
    {
        return key & ~InstanceMask;
    }

    // StaticId + Instance 로 다시 합치는 헬퍼
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
