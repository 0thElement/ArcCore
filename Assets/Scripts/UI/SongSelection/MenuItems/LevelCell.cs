using Zeroth.HierarchyScroll;
using UnityEngine;
using UnityEngine.UI;
using ArcCore.UI.Data;
using System.Collections;
using UnityEngine.EventSystems;

namespace ArcCore.UI.SongSelection
{
    public class LevelCell : CellBase, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private Text title;
        [SerializeField] private Text difficulty;
        [SerializeField] private Image isPlus;
        [SerializeField] private Image image;
        [SerializeField] private Image hoverOverlay;
        private Level level;
        private DifficultyItem diffItem;

        public override void SetCellData(CellDataBase cellDataBase)
        {
            LevelCellData songData = cellDataBase as LevelCellData;
            level = songData.level;
            title.text = songData.chart.Name;

            diffItem = new DifficultyItem(songData.chart);
            difficulty.text = diffItem.Text;
            if (diffItem.IsPlus) {
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