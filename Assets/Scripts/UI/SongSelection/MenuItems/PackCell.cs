using Zeroth.HierarchyScroll;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace ArcCore.UI.SongSelection
{
    public class PackCell : CellBase, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private Text packTitle;
        [SerializeField] private Image packImage;
        [SerializeField] private Text chartCount;
        [SerializeField] private Image hoverOverlay;
        //others

        private PackCell pack;

        public override void SetCellData(CellDataBase cellDataBase)
        {
            PackCellData packData = cellDataBase as PackCellData;
            this.pack = packData.pack;
            packTitle.text = packData.Pack.Name;
            chartCount.text = packData.chartCount;
            //TODO: set image
            //others
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
            SelectionMenu.Instance.SelectedPack = pack;
        }
    }
}