using Assets.Scripts.Data;

public static class ObjectKey
{
    private const int DomainShift = 24;
    private const int TypeShift = 16;
    private const int ClassShift = 8;

    public static uint Make(ObjectDomain domain, byte type, byte clazz, byte index)
    {
        uint value = 0u;

        value |= (uint)domain << DomainShift;
        value |= (uint)type << TypeShift;
        value |= (uint)clazz << ClassShift;
        value |= index;

        return value;
    }

    public static ObjectDomain GetDomain(uint key)
    {
        uint domain = (key >> DomainShift) & 0xFFu;
        return (ObjectDomain)domain;
    }

    public static byte GetTypeCode(uint key)
    {
        uint type = (key >> TypeShift) & 0xFFu;
        return (byte)type;
    }

    public static byte GetClassCode(uint key)
    {
        uint clazz = (key >> ClassShift) & 0xFFu;
        return (byte)clazz;
    }

    public static byte GetIndex(uint key)
    {
        uint index = key & 0xFFu;
        return (byte)index;
    }

    public static string ToHex8(uint key)
    {
        return key.ToString("X8");
    }
}
