using Zeroth.HierarchyScroll;
using UnityEngine;
using UnityEngine.UI;
using ArcCore.Storage.Data;
using ArcCore.Utilities;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using System.Collections.Generic;

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
        private Chart chart;
        private DifficultyItemData diffItem;

        private List<GameObject> diffItemObjects = new List<GameObject>();

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
            chart = songData.chart;
            image.sprite = null;

            title.text = chart.Name;
            difficulty.Set(chart);
            startOverlay.SetActive(SelectionMenu.Instance.SelectedLevel?.Id == level.Id);

            float x = difficultyItemPrefab.GetComponent<RectTransform>().anchoredPosition.x;
            RectTransform thisRect = GetComponent<RectTransform>();

            foreach (GameObject obj in diffItemObjects)
            {
                Destroy(obj);
            }
            diffItemObjects.Clear();

            foreach (Chart chart in level.Charts)
            {
                if (chart.DifficultyGroup != songData.chart.DifficultyGroup)
                {
                    GameObject obj = Instantiate(difficultyItemPrefab, thisRect);
                    RectTransform rect = obj.GetComponent<RectTransform>();
                    rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
                    x += rect.sizeDelta.x;

                    Image img = obj.GetComponent<Image>();
                    img.color = chart.DifficultyGroup.Color;

                    diffItemObjects.Add(obj);
                }
            }
        }

        protected override IEnumerator LoadCellFullyCoroutine(CellDataBase cellDataBase)
        {
            string path = level.GetRealPath(chart.ImagePath);
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
            if (SelectionMenu.Instance.SelectedLevel?.Id == level.Id)
            {
                SelectionMenu.Instance.SelectChart(level, chart);
            }
            else
                SelectionMenu.Instance.SelectedLevel = level;
        }
    }
}