using Zeroth.HierarchyScroll;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace ArcCore.UI.SongSelection
{
    public class SongCell : CellBase
    {
        //Text title
        //Text difficulty
        //Image image
        public override void SetCellData(CellDataBase cellDataBase)
        {
            SongCellData packData = cellDataBase as SongCellData;
            //TODO: make the prefab and complete this
        }

        protected override IEnumerator LoadCellFullyCoroutine(CellDataBase cellDataBase)
        {
            yield return null;
        }
    }
}