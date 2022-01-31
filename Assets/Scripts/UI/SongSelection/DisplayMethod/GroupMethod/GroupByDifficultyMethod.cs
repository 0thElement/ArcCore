
using System.Collections.Generic;
using Zeroth.HierarchyScroll;
using ArcCore.UI;
using System.Linq;
using ArcCore.Utilities;
using UnityEngine;

namespace ArcCore.UI.SongSelection
{
    public class GroupByDifficultyDisplayMethod : GroupLevelMethod
    {
        public override string MethodName { get => "Difficulty"; }
        protected override List<CellDataBase> GroupCells(List<LevelCellData> cells, SortLevelMethod sortMethod)
        {
            if (cells.Count == 0) return cells.ToList<CellDataBase>();

            List<(int, bool, List<LevelCellData>)> groups = new List<(int, bool, List<LevelCellData>)>();

            cells = cells
                .OrderBy(cell =>
                {
                    float cc = cell.chart.Constant;
                    (int diff, bool isPlus) = Conversion.CcToDifficulty(cc);
                    return (isPlus ? diff + 0.1f : diff);
                })
                .ThenBy(cell => cell.chart.Name)
                .ToList();

            //Sort to folders
            (int cdiff, bool cisPlus) = Conversion.CcToDifficulty(cells[0].chart.Constant);
            groups.Add((cdiff, cisPlus, new List<LevelCellData>()));

            foreach (LevelCellData level in cells)
            {
                (int diff, bool isPlus) = Conversion.CcToDifficulty(level.chart.Constant);
                if (diff != cdiff || cisPlus != isPlus)
                {
                    cdiff = diff;
                    cisPlus = isPlus;
                    groups.Add((diff, isPlus, new List<LevelCellData>()));
                }
                groups[groups.Count - 1].Item3.Add(level);
            }

            List<CellDataBase> folderCells = new List<CellDataBase>();
            foreach ((int diff, bool isPlus, List<LevelCellData> group) in groups)
            {
                SongFolderData newFolder = CreateFolder(diff, isPlus);
                newFolder.children = sortMethod.Convert(group).ToList<CellDataBase>();
                folderCells.Add(newFolder);
            }

            return folderCells;
        }
    }
}