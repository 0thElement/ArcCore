using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ArcCore.Storage.Data;
using ArcCore.Storage;

namespace Tests.StorageTests
{
    public class JsonInputTests
    {

        private Chart getDummyChart()
        {
            return new Chart {
                DifficultyGroup = JsonUserInput.Past,
                Difficulty = new Difficulty("1"),
                Constant = 1,
                SongPath = "",
                ImagePath = "",
                Background = "",
                Name = "",
                Artist = "",
                Bpm = "100",
                ChartPath = "",
                Style = Style.Light
            };
        }
        [Test]
        [TestCase(
            "1", 1,
            "testaudio", "testimg", "testbg",
            "testsong", "testnamer", "testartist", "testartistr",
            "100", "testcharter", "testillustrator",
            "testchart", "light"
        )]
        [TestCase(
            "1", 1,
            null, null, "testbg",
            "testsong", null, "testartist", null,
            "100", null, null,
            null, "conflict"
        )]
        [TestCase(
            "1", 1,
            "testaudio", "testimg", "testbg",
            "testsong", "testnamer", "testartist", "testartistr",
            "100", "testcharter", "testillustrator",
            "testchart", "nonexistent_style"
        )]
        public void ChartParse_Successful(
            string d, float constant,
            string song, string img, string bg,
            string name, string namer, string artist, string artistr,
            string bpm, string charter, string illust,
            string chartpath, string style
        )
        {
            (Chart actual, string json) = JsonHelper.GenerateChart(
                JsonUserInput.Past, d, constant, song, img, bg,
                name, namer, artist, artistr,
                bpm, charter, illust, chartpath, style
            );
            JObject jobj = JObject.Parse(json);
            Chart chart = JsonUserInput.ReadChartJson(jobj.CreateReader());

            Assert.That(
                actual.DifficultyGroup == chart.DifficultyGroup
                && actual.Difficulty.Name == chart.Difficulty.Name
                && actual.Difficulty.IsPlus == chart.Difficulty.IsPlus
                && actual.SongPath == chart.SongPath
                && actual.ImagePath == chart.ImagePath
                && actual.Name == chart.Name
                && actual.NameRomanized == chart.NameRomanized
                && actual.Artist == chart.Artist
                && actual.ArtistRomanized == chart.ArtistRomanized
                && actual.Bpm == chart.Bpm
                && actual.Constant == chart.Constant
                && actual.Charter == chart.Charter
                && actual.Illustrator == chart.Illustrator
                && actual.Background == chart.Background
                && actual.Style == chart.Style
                && actual.ChartPath == chart.ChartPath
                , json + "\n" + JsonHelper.GetJson(chart));
        }

        [TestCase(
            "1", 1,
            "testaudio", "testimg", "testbg",
            null, "testnamer", "testartist", "testartistr",
            "100", "testcharter", "testillustrator",
            "testchart", "light"
        )]
        [TestCase(
            "1", 1,
            "testaudio", "testimg", "testbg",
            "testsong", "testnamer", null, "testartistr",
            "100", "testcharter", "testillustrator",
            "testchart", "conflict"
        )]
        public void ChartParse_WithMissingValues_ThrowsError(
            string d, float constant,
            string song, string img, string bg,
            string name, string namer, string artist, string artistr,
            string bpm, string charter, string illust,
            string chartpath, string style
        )
        {
            (Chart actual, string json) = JsonHelper.GenerateChart(
                JsonUserInput.Future, d, constant, song, img, bg,
                name, namer, artist, artistr,
                bpm, charter, illust, chartpath, style
            );
            Assert.Throws<JsonReaderException>(() => {
                JObject jobj = JObject.Parse(json);
                Chart chart = JsonUserInput.ReadChartJson(jobj.CreateReader());
            });
        }


        [Test]
        [TestCase("past", "0.arc")]
        [TestCase("present", "1.arc")]
        [TestCase("future", "2.arc")]
        [TestCase("beyond", "3.arc")]
        public void ChartParse_WithCustomDifficulty_Success(string diff, string expectedChartPath)
        {
            DifficultyGroup dg = JsonUserInput.FromPreset(diff);

            Chart actual = getDummyChart();
            actual.DifficultyGroup = dg;
            actual.ChartPath = expectedChartPath;
            string json = JsonHelper.GetJson(actual);

            JObject jobj = JObject.Parse(json);
            Chart chart = JsonUserInput.ReadChartJson(jobj.CreateReader());

            Assert.That(
                chart.DifficultyGroup == dg
                && chart.ChartPath == expectedChartPath,
                json
            );
        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        public void LevelParse_Success(int chartCount)
        {
            Chart[] charts = new Chart[chartCount];
            for (int i=0; i < chartCount; i++) charts[i] = getDummyChart();

            (Level actual, string json) = JsonHelper.GenerateLevel("testpack", charts);
            JObject jobj = JObject.Parse(json);
            Level level = JsonUserInput.ReadLevelJson(jobj.CreateReader());

            Assert.That(
                level.PackExternalId == "testpack" && level.Charts.Length == chartCount,
                json
            );
        }

        [Test]
        public void LevelParse_NoCharts_ThrowsError()
        {
            (Level actual, string json) = JsonHelper.GenerateLevel("testpack", new Chart[0]);

            Assert.Throws<JsonReaderException>(() => {
                JObject jobj = JObject.Parse(json);
                Level level = JsonUserInput.ReadLevelJson(jobj.CreateReader());
            });
        }

        [Test]
        public void LevelParse_NoPacks_Success()
        {
            (Level actual, string json) = JsonHelper.GenerateLevel(null, new Chart[] { getDummyChart() });

            JObject jobj = JObject.Parse(json);
            Level level = JsonUserInput.ReadLevelJson(jobj.CreateReader());

            Assert.IsNull(level.PackExternalId);
        }

        [Test]
        public void PackParse_Success()
        {
            (Pack actual, string json) = JsonHelper.GeneratePack("testpack", "testimg");

            JObject jobj = JObject.Parse(json);
            Pack pack = JsonUserInput.ReadPackJson(jobj.CreateReader());

            Assert.That(
                pack.Name == "testpack"
                && pack.ImagePath == "testimg",
                json
            );
        }

        [Test]
        [TestCase("testpack", null)]
        [TestCase(null, "testimg")]
        [TestCase(null, null)]
        public void PackParse_WithMissingValue_ThrowsError(string name, string img)
        {
            (Pack actual, string json) = JsonHelper.GeneratePack(name, img);
            Assert.Throws<JsonReaderException>(() => {
                JObject jobj = JObject.Parse(json);
                Pack pack = JsonUserInput.ReadPackJson(jobj.CreateReader());
            });
        }
    }
}
