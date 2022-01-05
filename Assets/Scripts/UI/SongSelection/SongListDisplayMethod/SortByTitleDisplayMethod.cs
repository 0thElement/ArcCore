using System.Collections.Generic;
using Zeroth.HierarchyScroll;
using ArcCore.Serialization;
using System.Linq;
using ArcCore.Utitlities;
using UnityEngine;

namespace ArcCore.UI.SongSelection
{
    public class SortByDifficultyDisplayMethod : SortingDisplayMethod
    {
        public List<CellDataBase> SortCells(List<SongCellData> songCellDataList)
        {
            songCellDataList = songCellDataList
                        .OrderBy(cell => cell.chartInfo.Name)
                        .ToList();
            
            return songCellDataList;
        }
    }
}