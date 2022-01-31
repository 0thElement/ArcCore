using System.Collections.Generic;
using Zeroth.HierarchyScroll;
using ArcCore.Storage;
using System.Linq;
using ArcCore.Utilities;
using UnityEngine;

namespace ArcCore.UI.SongSelection
{
    public class SortByTitleMethod : SortLevelMethod
    {
        public override string MethodName { get => "Title"; }
        public override List<LevelCellData> Convert(List<LevelCellData> songCellDataList)
        {
            return songCellDataList
                    .OrderBy(cell => cell.chart.Name)
                    .ToList();
        }
    }
}