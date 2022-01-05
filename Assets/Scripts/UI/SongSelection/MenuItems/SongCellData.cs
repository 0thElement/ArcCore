
using Zeroth.HierarchyScroll;
using UnityEngine;
using UnityEngine.UI;
using ArcCore.UI;
using System.Collections.Generic;

namespace ArcCore.UI.SongSelection
{
    public class SongCellData : CellDataBase
    {
        public string name;
        public string difficulty;
        public bool isPlus = false;
        public List<Difficulty> diffList;
        public Level level;
    }
}