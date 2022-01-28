using Zeroth.HierarchyScroll;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using ArcCore.UI.Data;

namespace ArcCore.UI.SongSelection
{
    public class PackCell : CellBase, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private Text packTitle;
        [SerializeField] private Image packImage;
        [SerializeField] private Text chartCount;
        [SerializeField] private Image hoverOverlay;
        //others

        private Pack pack;

        public override void SetCellData(CellDataBase cellDataBase)
        {
            hoverOverlay.enabled = false;
            PackCellData packData = cellDataBase as PackCellData;
            pack = packData.pack;

            if (pack != null) {
                packTitle.text = pack.Name;
                //TODO: set image
                //others
            } 
            else
            {
                packTitle.text = "All";
            }
            chartCount.text = packData.chartCount.ToString();
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