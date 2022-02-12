using UnityEngine;
using UnityEngine.UI;
using ArcCore.Storage.Data;
using ArcCore.Utilities;
using System.Collections;
using UnityEngine.Networking;

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

                selectedTitle.text = chart.Name;
                selectedArtist.text = chart.Artist;
                selectedIllustrator.text = chart.Illustrator;
                selectedBpm.text = "BPM: " + chart.Bpm;
                selectedCharter.text = "Charter: " + chart.Charter;
                selectedScore.text = Conversion.ScoreDisplay(chart.PbScore.GetValueOrDefault());
                //TODO: set img of selectedGrade

                string jacketPath = selectedLevel.GetRealPath(chart.ImagePath);
                StartCoroutine(SetJacketCoroutine(jacketPath));
            }
        }

        public IEnumerator SetJacketCoroutine(string path)
        {
            UnityWebRequest www = UnityWebRequestTexture.GetTexture("file://" + path);
            yield return www.SendWebRequest();

            Texture2D tex = DownloadHandlerTexture.GetContent(www);
            selectedJacket.sprite = SpriteUtils.CreateCentered(tex);
        }

        public void Reset()
        {
            selectedJacket.sprite = null; //todo: default jacket
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