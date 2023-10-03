namespace CAT240Parser
{
    public class Cat240DataHeader
    {
        public byte Category { get; set; }
        public ushort Length { get; set; }
        public ushort FieldSpec { get; set; }
        public bool IsValidFieldSpec { get; set; }
        public List<bool> FieldSpecFlags { get; set; }

        public Cat240DataHeader(byte[] buffer)
        {
            int offset = 0;

            // CAT 标志位，1 byte
            Category = buffer[offset++];
            if (Category != 0xF0)
            {
                // TODO: Wrong package.
            }

            // Data block 长度，2 bytes
            Length = BitOperation.Get2BytesBigEndian(buffer, offset);
            offset += 2;

            // Field Specification, 2 bytes
            FieldSpec = BitOperation.Get2BytesBigEndian(buffer, offset);
            offset += 2;

            FieldSpecFlags = new List<bool>(16);
            for (int i = 15; i >= 0; i--)
            {
                FieldSpecFlags.Add(BitOperation.CheckBitPosition(FieldSpec, i));
            }

            IsValidFieldSpec = CheckFieldSpecs();
        }

        public bool CheckFieldSpecs()
        {
            //// Video Message 的场合
            bool isMandatoriesSet = false;
            bool isNeverPresentsSet = false;
            bool isVideoHeaderSet = false;
            bool isVideoBlockSet = false;

            // FRN 与 List 下标关系：FRN 1-7 时，下标为 FRN - 1； FRN 为 8 以上时，下标为 FRN

            // FRN 中 1，2，3，7，8 为强制项
            isMandatoriesSet = FieldSpecFlags[0] && FieldSpecFlags[1] && FieldSpecFlags[2] && FieldSpecFlags[6] &&
                               FieldSpecFlags[8];
            // FRN 中 4 为无需项
            isNeverPresentsSet = !FieldSpecFlags[3];
            // FRN 中 5，6 为二选一，9，10，11为三选一
            isVideoHeaderSet = ((FieldSpecFlags[4] ? 1 : 0) + (FieldSpecFlags[5] ? 1 : 0) == 1);
            isVideoBlockSet =
                ((FieldSpecFlags[9] ? 1 : 0) + (FieldSpecFlags[10] ? 1 : 0) + (FieldSpecFlags[11] ? 1 : 0) == 1);

            if (isMandatoriesSet && isNeverPresentsSet && isVideoHeaderSet && isVideoBlockSet)
            {
                return true;
            }

            //// Video Summary 的场合
            isMandatoriesSet = false;
            isNeverPresentsSet = false;
            isVideoHeaderSet = false;
            isVideoBlockSet = false;

            // FRN 中 1，2，4 为强制项
            isMandatoriesSet = FieldSpecFlags[0] && FieldSpecFlags[1] && FieldSpecFlags[3];
            // FRN 中 3, 5, 6, 7, 8, 9, 10, 11 为无需项
            isNeverPresentsSet = !(FieldSpecFlags[2] || FieldSpecFlags[4] || FieldSpecFlags[5] || FieldSpecFlags[6] ||
                                   FieldSpecFlags[8] || FieldSpecFlags[9] || FieldSpecFlags[10] || FieldSpecFlags[11]);
            if (isMandatoriesSet && isNeverPresentsSet)
            {
                return true;
            }

            return false;
        }

        public bool HasDataSourceIdentifier()
        {
            return FieldSpecFlags[0];
        }

        public bool HasMessageType()
        {
            return FieldSpecFlags[1];
        }

        public bool HasVideoRecordHeader()
        {
            return FieldSpecFlags[2];
        }

        public bool HasVideoSummary()
        {
            return FieldSpecFlags[3];
        }

        public bool HasVideoHeaderNano()
        {
            return FieldSpecFlags[4];
        }

        public bool HasVideoHeaderFemto()
        {
            return FieldSpecFlags[5];
        }

        public bool HasResolutionAndCompression()
        {
            return FieldSpecFlags[6];
        }

        public bool HasVideoBytesAndCellCounts()
        {
            return FieldSpecFlags[8];
        }

        public bool HasLowDataVolume()
        {
            return FieldSpecFlags[9];
        }

        public bool HasMediumDataVolume()
        {
            return FieldSpecFlags[10];
        }

        public bool HasHighDataVolume()
        {
            return FieldSpecFlags[11];
        }

        public bool HasTimeOfDay()
        {
            return FieldSpecFlags[12];
        }
    }
}
