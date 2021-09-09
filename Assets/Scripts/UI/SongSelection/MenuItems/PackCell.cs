using Zeroth.HierarchyScroll;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ArcCore.UI.SongSelection
{
    public class PackCell : CellBase
    {
        //Text packTitle
        //Image packImage
        //Text songCount
        //others
        public void SetCellData(CellDataBase cellDataBase)
        {
            PackCellData packData = cellDataBase as PackCellData;
        }

        public IEnumerator LoadCellFullyCoroutine(CellDataBase cellDataBase)
        {
        }
    }
}