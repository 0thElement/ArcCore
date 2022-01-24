using Zeroth.HierarchyScroll;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

namespace ArcCore.UI.SongSelection
{
    public class SongFolder : CellBase, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private Text title;
        [SerializeField] private Text count;
        [SerializeField] private Image hoverOverlay;

        public override void SetCellData(CellDataBase cellDataBase)
        {
            SongFolderData folderData = cellDataBase as SongFolderData;
            title.text = folderData.title;
            count.text = cellDataBase.children.Count.ToString();
        }

        protected override IEnumerator LoadCellFullyCoroutine(CellDataBase cellDataBase)
        {
            yield return null;
        }

        public void OnPointerDown(PointerEventData _)
        {
            hoverOverlay.enabled = true;
        }

        public void OnPointerUp(PointerEventData _)
        {
            hoverOverlay.enabled = false;
        }

        public void OnPointerClick(PointerEventData _)
        {
            ToggleCollapse();
        }
    }
}