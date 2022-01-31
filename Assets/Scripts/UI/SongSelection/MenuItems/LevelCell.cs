using Zeroth.HierarchyScroll;
using UnityEngine;
using UnityEngine.UI;
using ArcCore.Storage.Data;
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
        [SerializeField] private GameObject difficultyItemPrefab;
        private Level level;
        private DifficultyItemData diffItem;

        private void Awake()
        {
            SelectionMenu.Instance.OnLevelChange += (level) =>
            {
                startOverlay.SetActive(this.level != null && level != null && level.Id == this.level.Id);
            };
        }

        public override void SetCellData(CellDataBase cellDataBase)
        {
            hoverOverlay.enabled = false;
            LevelCellData songData = cellDataBase as LevelCellData;
            level = songData.level;
            title.text = songData.chart.Name;
            difficulty.Set(songData.chart);
            startOverlay.SetActive(SelectionMenu.Instance.SelectedLevel?.Id == level.Id);

            float x = difficultyItemPrefab.GetComponent<RectTransform>().anchoredPosition.x;
            RectTransform thisRect = GetComponent<RectTransform>();
            foreach (Chart chart in songData.level.Charts)
            {
                if (chart.DifficultyGroup != songData.chart.DifficultyGroup)
                {
                    GameObject obj = Instantiate(difficultyItemPrefab, thisRect);
                    RectTransform rect = obj.GetComponent<RectTransform>();
                    rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
                    x += rect.sizeDelta.x;

                    Image img = obj.GetComponent<Image>();
                    img.color = chart.DifficultyGroup.Color;
                }
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
            if (SelectionMenu.Instance.SelectedLevel?.Id == level.Id)
            {
                print("Start");
            }
            else
                SelectionMenu.Instance.SelectedLevel = level;
        }
    }
}