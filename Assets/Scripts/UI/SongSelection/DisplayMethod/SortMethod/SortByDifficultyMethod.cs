using System.Collections.Generic;
using Zeroth.HierarchyScroll;
using ArcCore.UI;
using System.Linq;
using ArcCore.Utilities;
using UnityEngine;

namespace ArcCore.UI.SongSelection
{
    public class SortByDifficultyMethod : SortLevelMethod
    {
        public override string MethodName { get => "Difficulty"; }
        public override List<LevelCellData> Convert(List<LevelCellData> songCellDataList)
        {
            if (songCellDataList.Count == 0) return songCellDataList;
            List<CellDataBase> folderCellDataList = new List<CellDataBase>();

            return songCellDataList
                        .OrderBy(cell =>
                        {
                            float cc = cell.chart.Constant;
                            (int diff, bool isPlus) = Conversion.CcToDifficulty(cc);
                            return (isPlus ? diff + 0.1f : diff);
                        })
                        .ThenBy(cell => cell.chart.Name)
                        .ToList();
        }
    }
}