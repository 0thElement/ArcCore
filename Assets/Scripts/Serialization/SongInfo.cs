namespace ArcCore.Serialization
{
    public class SongInfo
    {
        public string name;
        public string artist;
        public string illustrator;

        public string artFilename = "base.jpg";
        public string songFilename = "base.ogg";

        public float? baseBpm;
        public string displayBpm;

        public int previewStart;
        public int? previewEnd;

        public string sauce;
        //idfk
    }
}