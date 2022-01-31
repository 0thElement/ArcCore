using System.Collections.Generic;
using Zeroth.HierarchyScroll;
using ArcCore.UI;
using System.Linq;
using ArcCore.Utilities;
using UnityEngine;

namespace ArcCore.UI.SongSelection
{
    public class NoGroupingMethod : GroupLevelMethod
    {
        public override string MethodName { get => "None"; }
        protected override List<CellDataBase> GroupCells(List<LevelCellData> cells, SortLevelMethod sortMethod)
        {
            return sortMethod.Convert(cells).ToList<CellDataBase>();
        }
    }
}