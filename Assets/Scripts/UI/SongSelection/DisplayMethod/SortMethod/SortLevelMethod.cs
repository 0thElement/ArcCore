using UnityEngine;
using ArcCore.Storage.Data;
using System.Collections.Generic;
using Zeroth.HierarchyScroll;

namespace ArcCore.UI.SongSelection
{
    public abstract class SortLevelMethod
    {
        public abstract string MethodName { get; }

        public abstract List<LevelCellData> Convert(List<LevelCellData> cells);
    }
}