using CAT240Parser;

namespace RadarMonitor.Model
{
    public class Cat240Spec
    {
        public uint CellDuration { get; set; }
        public double VideoCellDurationUnit { get; set; }
        public bool IsDataCompressed { get; set; }
        public byte VideoResolution { get; set; }
        public ushort ValidBytesInDataBlock { get; set; }
        public uint ValidCellsInDataBlock { get; set; }
        public ushort VideoBlockLength { get; set; }
        public int MaxDistance { get; set; }

        public Cat240Spec(Cat240DataItems cat240DataItems)
        {
            CellDuration = cat240DataItems.CellDuration;
            VideoCellDurationUnit = cat240DataItems.VideoCellDurationUnit;
            IsDataCompressed = cat240DataItems.IsDataCompressed;
            VideoResolution = cat240DataItems.VideoResolution;
            ValidBytesInDataBlock = cat240DataItems.ValidBytesInDataBlock;
            ValidCellsInDataBlock = cat240DataItems.ValidCellsInDataBlock;
            VideoBlockLength = cat240DataItems.VideoBlockLength;
            MaxDistance = (int)(CellDuration * VideoCellDurationUnit * 300000 / 2 * ValidCellsInDataBlock);
        }
    }
}
