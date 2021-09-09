using Zeroth.HierarchyScroll;
using UnityEngine;
using UnityEngine.UI;

namespace ArcCore.UI.SongSelection
{
    public class SongCell : CellBase
    {
        //Text title
        //Text difficulty
        //Image image
        public void SetCellData(CellDataBase cellDataBase)
        {
            SongCellData packData = cellDataBase as SongCellData;
        }

        public IEnumerator LoadCellFullyCoroutine(CellDataBase cellDataBase)
        {
        }
    }
}