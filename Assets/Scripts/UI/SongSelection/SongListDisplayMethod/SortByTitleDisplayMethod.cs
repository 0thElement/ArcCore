using System.Collections.Generic;
using Zeroth.HierarchyScroll;
using ArcCore.Serialization;
using System.Linq;
using ArcCore.Utilities;
using UnityEngine;

namespace ArcCore.UI.SongSelection
{
    public class SortByTitleDisplayMethod : SortingDisplayMethod
    {
        protected override List<CellDataBase> SortCells(List<LevelCellData> songCellDataList)
        {
            return songCellDataList
                    .OrderBy(cell => cell.chart.Name)
                    .ToList<CellDataBase>();
        }
    }
}