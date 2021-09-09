
using Zeroth.HierarchyScroll;
using UnityEngine;
using UnityEngine.UI;
using ArcCore.Serialization;
using System.Collections.Generic;

namespace ArcCore.UI.SongSelection
{
    public class SongCellData : CellDataBase
    {
        public ChartInfo chartInfo;
        public List<DifficultyItem> diffList;
    }
}