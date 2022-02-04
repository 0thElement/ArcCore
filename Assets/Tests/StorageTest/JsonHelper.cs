using ArcCore.Utilities;
using ArcCore.Storage.Data;
using ArcCore.Storage;

namespace Tests.StorageTests
{
    public class JsonHelper
    {
        public static string PropertyOrNone(string prop, object val, bool comma = true)
        {
            if (val == null)
                return "";
            else return $@"""{prop}"": ""{val}""{(comma ? "," : "")}";
        }
        
        public static string GetJson(DifficultyGroup dg)
        {
            return $@"
            {{
                ""color"": ""{dg.Color.ToHexcode()}"",
                ""name"": ""{dg.Name}"",
                ""precedence"": ""{dg.Precedence}""
            }}";
        }

        public static string GetJson(Difficulty d)
        {
            return $"{d.Name}{(d.IsPlus ? "+" : "")}";
        }

        public static string GetJson(Chart c)
        {
            return $@"
            {{
                ""difficulty_group"": {GetJson(c.DifficultyGroup)},
                ""difficulty"": ""{GetJson(c.Difficulty)}"",
                {PropertyOrNone("constant", c.Constant)}
                {PropertyOrNone("song_path", c.SongPath)}
                {PropertyOrNone("image_path", c.ImagePath)}
                {PropertyOrNone("background", c.Background)}
                {PropertyOrNone("name", c.Name)}
                {PropertyOrNone("name_romanized", c.NameRomanized)}
                {PropertyOrNone("artist", c.Artist)}
                {PropertyOrNone("artist_romanized", c.ArtistRomanized)}
                {PropertyOrNone("bpm", c.Bpm)}
                {PropertyOrNone("charter", c.Charter)}
                {PropertyOrNone("illustrator", c.Illustrator)}
                {PropertyOrNone("chart_path", c.ChartPath)}
                {PropertyOrNone("style", c.Style.ToString().ToLower(), false)}
            }}";
        }
        public static string GetJson(Chart c, string difficulty_short)
        {
            return $@"
            {{
                {PropertyOrNone("diffculty_group", difficulty_short)}
                ""difficulty"": ""{GetJson(c.Difficulty)}"",
                {PropertyOrNone("constant", c.Constant)}
                {PropertyOrNone("song_path", c.SongPath)}
                {PropertyOrNone("image_path", c.ImagePath)}
                {PropertyOrNone("background", c.Background)}
                {PropertyOrNone("name", c.Name)}
                {PropertyOrNone("name_romanized", c.NameRomanized)}
                {PropertyOrNone("artist", c.Artist)}
                {PropertyOrNone("artist_romanized", c.ArtistRomanized)}
                {PropertyOrNone("bpm", c.Bpm)}
                {PropertyOrNone("charter", c.Charter)}
                {PropertyOrNone("illustrator", c.Illustrator)}
                {PropertyOrNone("chart_path", c.ChartPath)}
                {PropertyOrNone("style", c.Style.ToString().ToLower(), false)}
            }}";
        }

        public static (Chart chart, string json) GenerateChart(
            DifficultyGroup dg, string d, float constant,
            string song, string img, string bg,
            string name, string namer, string artist, string artistr,
            string bpm, string charter, string illust,
            string chartpath, string style
        )
        {
            Style s;
            switch (style.ToLower())
            {
                case "conflict":
                    s =  Style.Conflict;
                    break;
                case "light":
                default:
                    s = Style.Light;
                    break;
            }

            Chart chart = new Chart
            {
                DifficultyGroup = dg,
                Difficulty = new Difficulty(d),
                Constant = constant,
                SongPath = song ?? "base.ogg",
                ImagePath = img ?? "base.jpg",
                Background = bg ?? "bg.jpg",
                Name = name,
                NameRomanized = namer ?? name,
                Artist = artist,
                ArtistRomanized = artistr ?? artist,
                Bpm = bpm,
                Charter = charter,
                Illustrator = illust,
                ChartPath = chartpath ?? JsonUserInput.GetChartPathFromPresetGroup(dg),
                Style = s
            };
            return (chart, GetJson(chart));
        }

        public static (Level level, string json) GenerateLevel(string pack, Chart[] charts)
        {
            Level level = new Level {
                PackExternalId = pack,
                Charts = charts
            };

            string chartjson = "";
            foreach (Chart c in charts)
            {
                chartjson += GetJson(c)+",\n";
            }

            string json = $@"
            {{
                {PropertyOrNone("pack", pack)}
                ""charts"": [
                    {chartjson}
                ]
            }}";
            return (level, json);
        }

        public static (Pack pack, string json) GeneratePack(string name, string img)
        {
            Pack pack = new Pack {
                Name = name,
                ImagePath = img
            };
            string json = $@"
            {{
                {PropertyOrNone("name", name)}
                {PropertyOrNone("image_path", img, false)}
            }}";
            return (pack, json);
        }

        public static string GenerateSettingsJson(Level level, string exid)
        {
            (_, string levelJson) = GenerateLevel(level.PackExternalId, level.Charts);
            return $@"
            {{
                ""type"": ""level"",
                ""external_id"": ""{exid}"",
                ""data"": {levelJson}
            }}";
        }

        public static string GenerateSettingsJson(Pack pack, string exid)
        {
            (_, string packJson) = GeneratePack(pack.Name, pack.ImagePath);
            return $@"
            {{
                ""type"": ""pack"",
                ""external_id"": ""{exid}"",
                ""data"": {packJson}
            }}";
        }
    }
}