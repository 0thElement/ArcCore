using ArcCore.Storage.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ArcCore.UI.SongSelection
{
    public class DifficultyItem : MonoBehaviour
    {
        public static bool displayConstant = false;

        public RectTransform difficultyRect;
        public Text difficulty;
        public RectTransform plus;
        public Image background;
        public Color defaultColor;

        public void Set(Chart chart)
        {
            DifficultyItemData data = new DifficultyItemData(chart);
            if (displayConstant)
            {
                difficulty.text = chart.Constant.ToString();
                plus.gameObject.SetActive(false);
            }
            else
            {
                difficulty.text = data.Text;

                if (data.IsPlus)
                {
                    plus.gameObject.SetActive(true);
                    float diffWidth = LayoutUtility.GetPreferredWidth(difficultyRect);
                    float plusWidth = LayoutUtility.GetPreferredWidth(plus);
                    plus.anchoredPosition = new Vector2(diffWidth / 2 + plusWidth / 4, plus.anchoredPosition.y);
                    difficultyRect.anchoredPosition = new Vector2(-plusWidth / 4, difficultyRect.anchoredPosition.y);
                }
                else
                {
                    plus.gameObject.SetActive(false);
                    difficultyRect.anchoredPosition = new Vector2(0, difficultyRect.anchoredPosition.y);
                }
            }
            background.color = data.Color;
        }

        public void Reset()
        {
            difficulty.text = "";
            plus.gameObject.SetActive(false);
            background.color = defaultColor;
        }
    }
}