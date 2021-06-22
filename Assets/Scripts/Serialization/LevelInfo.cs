using Newtonsoft.Json;

namespace ArcCore.Serialization
{
    public class LevelInfo
    {
        [JsonRequired, JsonProperty(PropertyName = "namespace")]
        public string ns;
        public Style style;
        [JsonRequired]
        public SongInfo songInfo;
        [JsonRequired]
        public ChartInfo[] charts;
    }
}