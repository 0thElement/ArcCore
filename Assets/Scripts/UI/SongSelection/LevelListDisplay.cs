using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Zeroth.HierarchyScroll;
using ArcCore.Utilities;
using ArcCore.UI.Data;

namespace ArcCore.UI.SongSelection
{
    public class LevelListDisplay : MonoBehaviour
    {
        [SerializeField] private HierarchyScrollRect scrollRect;
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private GameObject folderPrefab;

        [SerializeField] private Image selectedJacket;
        [SerializeField] private Text  selectedTitle;
        [SerializeField] private Text  selectedArtist;
        [SerializeField] private Text  selectedIllustrator;
        [SerializeField] private Text  selectedCharter;
        [SerializeField] private Text  selectedBpm;
        [SerializeField] private Text  selectedScore;
        [SerializeField] private Image selectedGrade;

        private Level lastSelectedLevel;

        private ISongListDisplayMethod displayMethod = new SortByDifficultyDisplayMethod();

        public void Display(List<Level> levels, Level selectedLevel, DifficultyGroup selectedDiff)
        {
            //Set scroll list data
            List<CellDataBase> displayCells = displayMethod.Convert(levels, cellPrefab, folderPrefab, selectedDiff);
            scrollRect.SetData(displayCells);
            lastSelectedLevel = selectedLevel;
            //TODO: jump to selected level in scrollrect

            //Set selected level data
            if (selectedLevel == null) return;
            Chart chart = selectedLevel.GetExactChart(selectedDiff);
            //TODO: set img of selectedJacket
            selectedTitle.text = chart.Name;
            selectedArtist.text = chart.Artist;
            selectedIllustrator.text = chart.Illustrator;
            selectedBpm.text = "BPM: " + chart.Bpm;
            selectedCharter.text = "Charter: " + chart.Charter;
            selectedScore.text = Conversion.ScoreDisplay(chart.PbScore.Value);
            //TODO: set img of selectedGrade

            //if playanimation is true then animate the cell's scrolling to the new position
            //else play the entrance animation
        }
    }
}