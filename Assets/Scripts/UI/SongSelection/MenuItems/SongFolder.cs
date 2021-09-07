using Zeroth.HierarchyScroll;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace ArcCore.UI.SongSelection
{
    public class SongFolder : CellBase
    {
        //Text title
        //Text count
        public override void SetCellData(CellDataBase cellDataBase)
        {
            SongFolderData packData = cellDataBase as SongFolderData;
            //TODO: make the prefab and complete this
        }

        protected override IEnumerator LoadCellFullyCoroutine(CellDataBase cellDataBase)
        {
            yield return null;
        }
    }
}