using System.Text;

namespace CAT240Parser
{
    public class Cat240DataItems
    {
        public byte SystemAreaCode { get; set; }
        public byte SystemIdentificationCode { get; set; }
        public byte MessageType { get; set; }
        public uint MessageIndex { get; set; }
        public string VideoSummary { get; set; }
        public ushort StartAzimuth { get; set; }
        public double StartAzimuthInDegree { get; set; }
        public ushort EndAzimuth { get; set; }
        public double EndAzimuthInDegree { get; set; }
        public uint StartRange { get; set; }
        public uint CellDuration { get; set; }
        public double VideoCellDurationUnit { get; set; }
        public bool IsDataCompressed { get; set; }
        public byte VideoResolution { get; set; }
        public ushort ValidBytesInDataBlock { get; set; }
        public uint ValidCellsInDataBlock { get; set; }
        public ushort VideoBlockLength { get; set; }
        public List<byte> VideoBlocks { get; set; }
        public uint TimeOfDay { get; set; }
        public uint TimeOfDayInSec { get; set; }

        private const double AzimuthUnit = 360.0 / 65536;

        public Cat240DataItems(Cat240DataHeader header, byte[] buffer, long size)
        {
            VideoSummary = string.Empty;

            int offset = 5;

            // Data source identifier, 2 bytes (SAC / SIC)
            if (header.HasDataSourceIdentifier())
            {
                SystemAreaCode = buffer[offset++];
                SystemIdentificationCode = buffer[offset++];
            }

            // Message type, 1 byte (001-Summary, 002-Video)
            if (header.HasMessageType())
            {
                MessageType = buffer[offset++];
            }

            // Message index, 4 bytes
            if (header.HasVideoRecordHeader())
            {
                MessageIndex = BitOperation.Get4BytesBigEndian(buffer, offset);
                offset += 4;
            }

            // Video summary, 1 + n bytes
            if (header.HasVideoSummary())
            {
                byte charCount = buffer[offset++];

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < charCount; i++)
                {
                    sb.Append(buffer[offset++]);
                }

                VideoSummary = sb.ToString();
            }

            // Video header (Nano or Femto)
            VideoCellDurationUnit = 1e-15;
            if (header.HasVideoHeaderNano())
            {
                VideoCellDurationUnit = 1e-9;
            }

            // Start azimuth, 2 bytes, 单位：360/2^16
            StartAzimuth = BitOperation.Get2BytesBigEndian(buffer, offset);
            offset += 2;
            StartAzimuthInDegree = StartAzimuth * AzimuthUnit;

            // End azimuth, 2 bytes, 单位：360/2^16
            EndAzimuth = BitOperation.Get2BytesBigEndian(buffer, offset);
            offset += 2;
            EndAzimuthInDegree = EndAzimuth * AzimuthUnit;

            // Start range, 4 bytes
            StartRange = BitOperation.Get4BytesBigEndian(buffer, offset);
            offset += 4;

            // Cell duration, 4 bytes (Nano or Femto)
            CellDuration = BitOperation.Get4BytesBigEndian(buffer, offset);
            offset += 4;

            // Data compression indicator, 1 byte
            if (header.HasResolutionAndCompression())
            {
                byte compress_flag = buffer[offset++];
                IsDataCompressed = (compress_flag == 0b10000000);

                byte resolution_flag = buffer[offset++];
                VideoResolution = (byte)Math.Pow(2.0, resolution_flag - 1);
            }

            // Valid video bytes and cells
            if (header.HasVideoBytesAndCellCounts())
            {
                // Valid bytes in Video block data, 2 bytes
                ValidBytesInDataBlock = BitOperation.Get2BytesBigEndian(buffer, offset);
                offset += 2;

                // Valid cells in Video block data, 3 bytes
                ValidCellsInDataBlock = BitOperation.Get3BytesBigEndian(buffer, offset);
                offset += 3;
            }

            // Video block data volume count, 1 byte
            byte videoBlockCount = buffer[offset++];
            if (header.HasLowDataVolume())
            {
                // Video block low data volume, 4 * n bytes
                VideoBlockLength = 4;
            }
            else if (header.HasMediumDataVolume())
            {
                // Video block medium data volume, 64 * n bytes
                VideoBlockLength = 64;
            }
            else if (header.HasHighDataVolume())
            {
                // Video block medium data volume, 256 * n bytes
                VideoBlockLength = 256;
            }
            VideoBlocks = new List<byte>(videoBlockCount * VideoBlockLength);

            for (byte i = 0; i < videoBlockCount; i++)
            {
                ArraySegment<byte> segment = new ArraySegment<byte>(buffer, offset, VideoBlockLength);
                offset += VideoBlockLength;

                VideoBlocks.AddRange(segment.ToList());
            }

            //int count = videoBlockCount * VideoBlockLength;
            //VideoBlocks = new List<byte>(count);
            //ArraySegment<byte> segment = new ArraySegment<byte>(buffer, offset, count);
            //offset += count;
            //VideoBlocks.AddRange(segment.ToList());

            // Time of day, 3 bytes, 单位 1/128 s
            if (header.HasTimeOfDay())
            {
                TimeOfDay = BitOperation.Get3BytesBigEndian(buffer, offset);
                offset += 3;

                TimeOfDayInSec = TimeOfDay / 128;
            }
        }

        public uint GetCellData(int cellIndex)
        {
            byte[] buffer = VideoBlocks.ToArray();
            switch (VideoResolution)
            {
                case 1:
                    bool cell1Bit = BitOperation.Get1BitBigEndian(buffer, cellIndex / 8, cellIndex);
                    return cell1Bit ? (uint)1 : 0;
                case 2:
                    byte cell2Bits = BitOperation.Get2BitsBigEndian(buffer, cellIndex / 4, cellIndex * 2);
                    return (uint)cell2Bits;
                case 4:
                    byte cell4Bits = BitOperation.Get4BitsBigEndian(buffer, cellIndex / 2, cellIndex * 4);
                    return (uint)cell4Bits;
                case 8:
                    byte cell1Byte = buffer[cellIndex];
                    return (uint)cell1Byte;
                case 16:
                    ushort cell2Bytes = BitOperation.Get2BytesBigEndian(buffer, cellIndex * 2);
                    return (uint)cell2Bytes;
                case 32:
                    uint cell4Bytes = BitOperation.Get4BytesBigEndian(buffer, cellIndex * 4);
                    return cell4Bytes;
                default:
                    byte cellDefault = buffer[cellIndex];
                    return (uint)cellDefault;
            }
        }

        public uint GetCellData(byte[] buffer, int cellIndex)
        {
            switch (VideoResolution)
            {
                case 1:
                    bool cell1Bit = BitOperation.Get1BitBigEndian(buffer, cellIndex / 8, cellIndex);
                    return cell1Bit ? (uint)1 : 0;
                case 2:
                    byte cell2Bits = BitOperation.Get2BitsBigEndian(buffer, cellIndex / 4, cellIndex * 2);
                    return (uint)cell2Bits;
                case 4:
                    byte cell4Bits = BitOperation.Get4BitsBigEndian(buffer, cellIndex / 2, cellIndex * 4);
                    return (uint)cell4Bits;
                case 8:
                    byte cell1Byte = buffer[cellIndex];
                    return (uint)cell1Byte;
                case 16:
                    ushort cell2Bytes = BitOperation.Get2BytesBigEndian(buffer, cellIndex * 2);
                    return (uint)cell2Bytes;
                case 32:
                    uint cell4Bytes = BitOperation.Get4BytesBigEndian(buffer, cellIndex * 4);
                    return cell4Bytes;
                default:
                    byte cellDefault = buffer[cellIndex];
                    return (uint)cellDefault;
            }
        }

        public bool IsSpecChanged(Cat240DataItems other)
        {
            if (other == null)
            {
                return true;
            }

            //if (this.CellDuration != other.CellDuration)
            //{
            //    return true;
            //}

            //if (this.VideoCellDurationUnit != other.VideoCellDurationUnit)
            //{
            //    return true;
            //}

            //if (this.IsDataCompressed != other.IsDataCompressed)
            //{
            //    return true;
            //}

            //if (this.VideoResolution != other.VideoResolution)
            //{
            //    return true;
            //}

            //if (this.ValidBytesInDataBlock != other.ValidBytesInDataBlock)
            //{
            //    return true;
            //}

            //if (this.ValidCellsInDataBlock != other.ValidCellsInDataBlock)
            //{
            //    return true;
            //}

            //if (this.VideoBlockLength != other.VideoBlockLength)
            //{
            //    return true;
            //}
            if (this.CellDuration != other.CellDuration
                || this.VideoCellDurationUnit != other.VideoCellDurationUnit
                || this.IsDataCompressed != other.IsDataCompressed
                || this.VideoResolution != other.VideoResolution
                || this.ValidBytesInDataBlock != other.ValidBytesInDataBlock
                || this.ValidCellsInDataBlock != other.ValidCellsInDataBlock
                || this.VideoBlockLength != other.VideoBlockLength)
            {
                return true;
            }

            return false;
        }
    }
}
