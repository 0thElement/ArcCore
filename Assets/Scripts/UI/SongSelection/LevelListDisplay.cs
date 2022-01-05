using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Zeroth.HierarchyScroll;
using ArcCore.Serialization;

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

        public void Display(List<Level> levels, Level selectedLevel, Difficulty selectedDiff, bool playAnimation = false)
        {
            List<CellDataBase> displayCells = displayMethod.FromSongList(toDisplay, cellPrefab, folderPrefab, selectedDiff);
            scrollRect.SetData(displayCells);

            Chart chart = selectedLevel.GetClosestDifficulty(selectedDiff);
            //TODO: set img of selectedJacket
            selectedTitle.text = chart.Name;
            selectedArtist.text = chart.Artist;
            selectedBpm.text = chart.Bpm;
            selectedScore = chart.PbScore;
            //TODO: set img of selectedGrade

            //if playanimation is true then animate the cell's scrolling to the new position
            //else play the entrance animation
        }
    }
}