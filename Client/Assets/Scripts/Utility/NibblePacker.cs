// -----------------------------------------------------------------------------
// DomainKey의 Class(8bit) 필드에 들어가는 값의 인코딩/디코딩 규약 유틸.
// - 이 유틸은 "Class 바이트"의 비트 패킹만 책임진다.
// - DomainKey 조립/해석은 DomainKey가 담당한다.
// -----------------------------------------------------------------------------

namespace Assets.Scripts.Utility
{
    public static class NibblePacker
    {
        public static byte Pack(byte mainIndex, byte subIndex)
        {
            mainIndex = (byte)(mainIndex & 0x0F);
            subIndex = (byte)(subIndex & 0x0F);
            return (byte)((mainIndex << 4) | subIndex);
        }

        public static void Unpack(byte packed, out byte mainIndex, out byte subIndex)
        {
            mainIndex = (byte)((packed >> 4) & 0x0F);
            subIndex = (byte)(packed & 0x0F);
        }
    }
}
