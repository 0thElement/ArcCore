using System.Collections.Generic;
using ArcCore.Serialization;
using ArcCore.UI.Data;
using Zeroth.HierarchyScroll;
using UnityEngine;

namespace ArcCore.UI.SongSelection
{
    public interface ISongListDisplayMethod
    {
        List<CellDataBase> Convert(List<Level> toDisplay, GameObject folderPrefab, GameObject cellPrefab, DifficultyGroup selectedDiff);

    }
}