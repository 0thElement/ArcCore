using Newtonsoft.Json;

namespace ArcCore.Serialization
{
    public class ChartInfo
    {
        [JsonConstructor]
        public ChartInfo()
        {
            if(filename == null)
            {
                if (diffType == DifficultyType.Past)
                    filename = "0.arc";
                else if (diffType == DifficultyType.Present)
                    filename = "1.arc";
                else if (diffType == DifficultyType.Future)
                    filename = "2.arc";
                else if (diffType == DifficultyType.Beyond)
                    filename = "3.arc";
            }
        }

        public string charter;
        public string notes = "";

        public string filename;

        public string difficulty;
        public ushort cc = 0;
        public DifficultyType diffType;

        public Style styleOverride;
        public SongInfo songInfoOverride;

        //public string lua; //0th why
    }


}