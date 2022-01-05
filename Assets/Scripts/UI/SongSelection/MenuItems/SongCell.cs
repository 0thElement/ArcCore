using Zeroth.HierarchyScroll;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace ArcCore.UI.SongSelection
{
    public class SongCell : CellBase, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private Text title;
        [SerializeField] private Text difficulty;
        [SerializeField] private Image isPlus;
        [SerializeField] private Image image;
        [SerializeField] private Image hoverOverlay;
        private Level level;

        public override void SetCellData(CellDataBase cellDataBase)
        {
            SongCellData songData = cellDataBase as SongCellData;
            title.text = songData.name;
            difficulty.text = songData.difficulty;
            if (songData.isPlus) {
                //TODO: enable isPlus img
            }
        }

        protected override IEnumerator LoadCellFullyCoroutine(CellDataBase cellDataBase)
        {
            //TODO: Set image
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
            SelectionMenu.Instance.SelectedLevel = level;
        }
    }
}