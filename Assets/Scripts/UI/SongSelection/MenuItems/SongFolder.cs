using Zeroth.HierarchyScroll;
using UnityEngine;
using UnityEngine.UI;

namespace ArcCore.UI.SongSelection
{
    public class SongFolder : CellBase
    {
        //Text title
        //Text count
        public void SetCellData(CellDataBase cellDataBase)
        {
            SongFolderData packData = cellDataBase as SongFolderData;
        }

        public IEnumerator LoadCellFullyCoroutine(CellDataBase cellDataBase)
        {
        }
    }
}