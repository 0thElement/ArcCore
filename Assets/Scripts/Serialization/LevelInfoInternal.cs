namespace ArcCore.Serialization
{
    public class LevelInfoInternal
    {
        public ChartInfo[] charts;
        public string[] importedGlobals;

        public LevelInfoInternal(LevelInfo levelInfo, string[] importedGlobals)
        {
            charts = levelInfo.charts;

            for (int i = 0; i < charts.Length; i++)
            {
                charts[i].cStyle = charts[i].cStyle ?? levelInfo.style;
                charts[i].cSongInfo = charts[i].cSongInfo ?? levelInfo.songInfo;
            }

            this.importedGlobals = importedGlobals;
        }
    }
}