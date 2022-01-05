using System.Collections.Generic;
using ArcCore.Serialization;
using Zeroth.HierarchyScroll;
using UnityEngine;

namespace ArcCore.UI.SongSelection
{
    public interface ISongListDisplayMethod
    {
        List<CellDataBase> Convert(List<Chart> toDisplay, GameObject folderPrefab, GameObject cellPrefab);
    }
}