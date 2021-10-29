namespace ArcCore.Serialization
{
    public class ChartInfo
    {
        public string charter;
        public string notes; //kms

        public string filename;

        public string difficulty;
        public ushort cc = 0;
        public DifficultyType diffType;

        public Style cStyle;
        public SongInfo cSongInfo;

        public string lua; //0th why
    }
}