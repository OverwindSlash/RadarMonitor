namespace CAT240Parser
{
    public class BitOperation
    {
        public static bool CheckBitPosition(ushort content, int position)
        {
            return (content & (1 << position)) != 0;
        }

        public static bool Get1BitBigEndian(byte[] buffer, int index, int startBit)
        {
            int bitOffset = startBit % 8; // 起始 bit 位在字节中的偏移量
            return (buffer[index] & (1 << (7 - bitOffset))) != 0;
        }

        public static byte Get2BitsBigEndian(byte[] buffer, int index, int startBit)
        {
            int bitOffset = startBit % 8; // 起始 bit 位在字节中的偏移量
            int bit2 = (buffer[index] >> (6 - bitOffset)) & 0b11;
            return (byte)bit2;
        }

        public static byte Get4BitsBigEndian(byte[] buffer, int index, int startBit)
        {
            int bitOffset = startBit % 8; // 起始 bit 位在字节中的偏移量
            int bit4 = (buffer[index] >> (4 - bitOffset)) & 0b1111;
            return (byte)bit4;
        }

        public static ushort Get2BytesBigEndian(byte[] buffer, int index)
        {
            return (ushort)((buffer[index] << 8) | buffer[index + 1]);
        }

        public static uint Get3BytesBigEndian(byte[] buffer, int index)
        {
            return (uint)((buffer[index] << 16) | (buffer[index + 1] << 8) | buffer[index + 2]);
        }

        public static uint Get4BytesBigEndian(byte[] buffer, int index)
        {
            return (uint)((buffer[index] << 24) | (buffer[index + 1] << 16) | (buffer[index + 2] << 8) | buffer[index + 3]);
        }
    }
}
