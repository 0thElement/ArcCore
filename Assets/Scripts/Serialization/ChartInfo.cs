using Newtonsoft.Json;

namespace ArcCore.Serialization
{
    public class ChartInfo
    {
        public string charter;
        public string notes = "";

        public string filename;

        public ushort cc = 0;
        public DifficultyType diffType;

        public StyleScheme styleOverride;
        public SongInfo songInfoOverride;
    }
}