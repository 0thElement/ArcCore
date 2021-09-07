using UnityEngine;
using System.Collections;

namespace Zeroth.HierarchyScroll
{
    ///<summary>
    ///Base class for cells. User inherits this class and configure them to create the cell style they want
    ///</summary>
    public abstract class CellBase : MonoBehaviour
    {
        //Used by to callback to hierarchy scroll rect to collapse this cell
        [HideInInspector] public HierarchyScrollRect scrollRect; 
        [HideInInspector] public HierarchyCellData hierarchyData;

        ///<summary>
        ///Toggle collapse of this cell
        ///</summary>
        public void ToggleCollapse()
        {
            scrollRect.ToggleCollapse(hierarchyData.indexFlat);
        }

        ///<summary>
        ///Add data to cell. Called whenever cell goes into viewport
        ///</summary>
        public abstract void SetCellData(CellDataBase cellData);


        ///<summary>
        ///Finalize cell loading. Called when player scrolls slower than a set threshold for a set duration of time
        ///Intended for costly processes such as loading image texture
        ///</summary>
        public void SetCellDataFully(CellDataBase cellData)
        {
            StartCoroutine(LoadCellFullyCoroutine(cellData));
        }
        public void ClearCell()
        {
            StopAllCoroutines();
        }
        protected abstract IEnumerator LoadCellFullyCoroutine(CellDataBase cellData);
    }
}