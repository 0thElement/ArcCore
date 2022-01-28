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
        [SerializeField] private DifficultyItem difficulty;
        [SerializeField] private Image image;
        [SerializeField] private Image hoverOverlay;
        [SerializeField] private GameObject startOverlay;
        private Level level;
        private DifficultyItemData diffItem;

        public override void SetCellData(CellDataBase cellDataBase)
        {
            hoverOverlay.enabled = false;
            LevelCellData songData = cellDataBase as LevelCellData;
            level = songData.level;
            title.text = songData.chart.Name;
            difficulty.Set(songData.chart);
            startOverlay.SetActive(SelectionMenu.Instance.SelectedLevel?.Id == level.Id);
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
            if (SelectionMenu.Instance.SelectedLevel?.Id == level.Id)
            {
                print("Start");
            }
            else
                SelectionMenu.Instance.SelectedLevel = level;
        }
    }
}