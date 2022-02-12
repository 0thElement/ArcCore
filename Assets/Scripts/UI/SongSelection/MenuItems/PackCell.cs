using Zeroth.HierarchyScroll;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using ArcCore.Storage.Data;
using ArcCore.Utilities;
using UnityEngine.Networking;

namespace ArcCore.UI.SongSelection
{
    public class PackCell : CellBase, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private Text packTitle;
        [SerializeField] private Image image;
        [SerializeField] private Text chartCount;
        [SerializeField] private Image hoverOverlay;

        private Pack pack;

        public override void SetCellData(CellDataBase cellDataBase)
        {
            hoverOverlay.enabled = false;
            PackCellData packData = cellDataBase as PackCellData;
            pack = packData.pack;

            image.sprite = null;

            if (pack != null) {
                packTitle.text = pack.Name;
            } 
            else
            {
                packTitle.text = "All";
            }
            chartCount.text = packData.chartCount.ToString();
        }

        protected override IEnumerator LoadCellFullyCoroutine(CellDataBase cellDataBase)
        {
            if (pack == null) yield break;

            string path = pack.GetRealPath(pack.ImagePath);
            UnityWebRequest www = UnityWebRequestTexture.GetTexture("file://" + path);
            yield return www.SendWebRequest();

            Texture2D tex = DownloadHandlerTexture.GetContent(www);
            image.sprite = SpriteUtils.CreateCentered(tex);
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