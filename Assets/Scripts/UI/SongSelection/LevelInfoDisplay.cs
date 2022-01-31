using UnityEngine;
using UnityEngine.UI;
using ArcCore.Storage.Data;
using ArcCore.Utilities;

namespace ArcCore.UI.SongSelection
{
    public class LevelInfoDisplay : MonoBehaviour
    {
        [SerializeField] private Image selectedJacket;
        [SerializeField] private Text  selectedTitle;
        [SerializeField] private Text  selectedArtist;
        [SerializeField] private Text  selectedIllustrator;
        [SerializeField] private Text  selectedCharter;
        [SerializeField] private Text  selectedBpm;
        [SerializeField] private Text  selectedScore;
        [SerializeField] private Image selectedGrade;

        public void Display(Level selectedLevel, DifficultyGroup selectedDiff)
        {
            //Set selected level data
            if (selectedLevel == null) Reset();
            else
            {
                Chart chart = selectedLevel.GetExactChart(selectedDiff);
                //TODO: set img of selectedJacket
                selectedTitle.text = chart.Name;
                selectedArtist.text = chart.Artist;
                selectedIllustrator.text = chart.Illustrator;
                selectedBpm.text = "BPM: " + chart.Bpm;
                selectedCharter.text = "Charter: " + chart.Charter;
                selectedScore.text = Conversion.ScoreDisplay(chart.PbScore.Value);
                //TODO: set img of selectedGrade
            }
        }

        public void Reset()
        {
            //TODO: set img of selectedJacket
            selectedTitle.text = "";
            selectedArtist.text = "";
            selectedIllustrator.text = "";
            selectedBpm.text = "";
            selectedCharter.text = "";
            selectedScore.text = Conversion.ScoreDisplay(0);
            //TODO: set img of selectedGrade
        }
    }
}