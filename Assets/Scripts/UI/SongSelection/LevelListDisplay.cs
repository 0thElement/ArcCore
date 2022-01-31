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

        private Level lastSelectedLevel;

        private ISongListDisplayMethod displayMethod = new SortByDifficultyDisplayMethod();

        public void Display(List<Level> levels, Level selectedLevel, DifficultyGroup selectedDiff)
        {
            //Set scroll list data
            List<CellDataBase> displayCells = displayMethod.Convert(levels, cellPrefab, folderPrefab, selectedDiff);
            scrollRect.SetData(displayCells);
            lastSelectedLevel = selectedLevel;
            //TODO: jump to selected level in scrollrect

            //if playanimation is true then animate the cell's scrolling to the new position
            //else play the entrance animation
        }
    }
}