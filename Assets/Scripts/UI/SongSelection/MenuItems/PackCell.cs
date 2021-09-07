using Zeroth.HierarchyScroll;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace ArcCore.UI.SongSelection
{
    public class PackCell : CellBase
    {
        //Text packTitle
        //Image packImage
        //Text songCount
        //others
        public override void SetCellData(CellDataBase cellDataBase)
        {
            PackCellData packData = cellDataBase as PackCellData;
            //TODO: make the prefab and complete this
        }

        protected override IEnumerator LoadCellFullyCoroutine(CellDataBase cellDataBase)
        {
            yield return null;
        }
    }
}