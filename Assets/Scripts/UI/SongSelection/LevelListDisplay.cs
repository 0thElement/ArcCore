using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Zeroth.HierarchyScroll;
using ArcCore.Serialization;
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
        [SerializeField] private Text  selectedBpm;
        [SerializeField] private Text  selectedScore;
        [SerializeField] private Image selectedGrade;

        private ISongListDisplayMethod displayMethod = new SortByDifficultyDisplayMethod();

        public void Display(List<Level> levels, Level selectedLevel, DifficultyGroup selectedDiff, bool playAnimation = false)
        {
            List<CellDataBase> displayCells = displayMethod.Convert(levels, cellPrefab, folderPrefab, selectedDiff);
            scrollRect.SetData(displayCells);

            Chart chart = selectedLevel.GetClosestChart(selectedDiff);
            //TODO: set img of selectedJacket
            selectedTitle.text = chart.Name;
            selectedArtist.text = chart.Artist;
            selectedBpm.text = chart.Bpm;
            selectedScore.text = chart.PbScore.Value.ToString("D8");
            //TODO: set img of selectedGrade

            //if playanimation is true then animate the cell's scrolling to the new position
            //else play the entrance animation
        }
    }
}