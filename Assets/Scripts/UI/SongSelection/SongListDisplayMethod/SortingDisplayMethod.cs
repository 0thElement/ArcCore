using UnityEngine;
using ArcCore.UI.Data;
using System.Collections.Generic;
using Zeroth.HierarchyScroll;

namespace ArcCore.UI.SongSelection
{
    public abstract class SortingDisplayMethod : ISongListDisplayMethod
    {
        protected GameObject folderPrefab;
        protected SongFolderData CreateFolder(int diff, bool isPlus)
        {
            return new SongFolderData
            {
                title = diff.ToString() + (isPlus ? "+" : ""),
                children = new List<CellDataBase>(),
                prefab = folderPrefab
            };
        }

        public List<CellDataBase> Convert(List<Level> toDisplay, GameObject cellPrefab, GameObject folderPrefab, DifficultyGroup selectedDiff)
        {
            this.folderPrefab = folderPrefab;

            List<LevelCellData> difficultyMatchLevelCells = new List<LevelCellData>();
            List<LevelCellData> otherLevelCells = new List<LevelCellData>();

            if (toDisplay.Count == 0) return null;

            foreach (Level level in toDisplay)
            {
                Chart matchingChart = null;    
                foreach (Chart chart in level.Charts)
                {
                    if (chart.DifficultyGroup.Precedence == selectedDiff.Precedence) {
                        matchingChart = chart;
                    }
                }

                if (matchingChart != null)
                {
                    difficultyMatchLevelCells.Add(new LevelCellData
                    {
                        prefab = cellPrefab,
                        chart = matchingChart,
                        level = level,
                    });
                } else {
                    otherLevelCells.Add(new LevelCellData
                    {
                        prefab = cellPrefab,
                        level = level,
                        chart = level.GetClosestChart(selectedDiff),
                    });
                }
            }

            List<CellDataBase> result = SortCells(difficultyMatchLevelCells);
            List<CellDataBase> otherDifficulties = SortCells(otherLevelCells);

            SongFolderData otherDifficultiesFolder = new SongFolderData
            {
                title = "Other Difficulties",
                children = otherDifficulties,
                prefab = folderPrefab
            };
            result.Add(otherDifficultiesFolder);


            return result;
        }

        protected abstract List<CellDataBase> SortCells(List<LevelCellData> cells);
    }
}