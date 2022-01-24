namespace ArcCore.UI
{
    public class LevelInfoInternal
    {
        public ChartInfo[] charts;
        public string[] importedGlobals;

        public LevelInfoInternal(ChartInfo[] charts, string[] importedGlobals)
        {
            this.charts = charts;
            this.importedGlobals = importedGlobals;
        }

        public ChartInfo GetClosestDifficulty(float prioritizedDifficulty)
        {
            ChartInfo result = null;
            float closestDifference = float.PositiveInfinity;

            foreach (ChartInfo chart in charts)
            {
                if (chart.diffType.sortOrder - prioritizedDifficulty < closestDifference)
                    result = chart;
            }

            return result;
        }
    }
}