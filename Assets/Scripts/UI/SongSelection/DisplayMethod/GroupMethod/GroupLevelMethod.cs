using System.Collections.Generic;
using ArcCore.Storage;
using ArcCore.Storage.Data;
using Zeroth.HierarchyScroll;
using UnityEngine;

namespace ArcCore.UI.SongSelection
{
    public abstract class GroupLevelMethod
    {
        private GameObject folderPrefab;
        public abstract string MethodName { get; }

        protected SongFolderData CreateFolder(int diff, bool isPlus)
        {
            return new SongFolderData
            {
                title = diff.ToString() + (isPlus ? "+" : ""),
                children = new List<CellDataBase>(),
                prefab = folderPrefab
            };
        }

        public List<CellDataBase> Convert(
            List<Level> toDisplay, GameObject cellPrefab,
            DifficultyGroup selectedDiff,
            SortLevelMethod sortMethod, GameObject folderPrefab)
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
                    if (chart.DifficultyGroup == selectedDiff) {
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

            List<CellDataBase> result = GroupCells(difficultyMatchLevelCells, sortMethod);
            List<CellDataBase> otherDifficultiesGroup = GroupCells(otherLevelCells, sortMethod);

            SongFolderData otherDifficultiesFolder = new SongFolderData
            {
                title = "Other Difficulties",
                children = otherDifficultiesGroup,
                prefab = folderPrefab
            };
            result.Add(otherDifficultiesFolder);

            return result;
        }

        protected abstract List<CellDataBase> GroupCells(List<LevelCellData> cells, SortLevelMethod sortMethod);
    }
}