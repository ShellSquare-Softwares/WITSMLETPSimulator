namespace ETPSimulatorApp
{
    public class Settings
    {
        public bool BetweenTimeIndex { get; set; }
        public long StartTime { get; set; }
        public long EndTime { get; set; }
        public bool BetweenDepthIndex { get; set; }
        public long StartDepth { get; set; }
        public long EndDepth { get; set; }
        public bool ByIndexCount { get; set; }
        public int IndexCount { get; set; }
        public double StartDepthInput { get; set; }
        public double EndDepthInput { get; set; }
        public string StartTimeInput { get; set; }
        public string EndTimeInput { get; set; }
    }
}
