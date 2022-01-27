namespace ArcCore.UI.Data
{
    public class ChartSettings
    {
        public int Offset { get; set; }
        public float ChartSpeed { get; set; }

        public static ChartSettings DefaultChartSettings
            => new ChartSettings
            {
                Offset = 0,
                ChartSpeed = 1
            };
    }
}