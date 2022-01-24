using System.Linq;
using System.Collections.Generic;
using Zeroth.HierarchyScroll;
using ArcCore.Serialization;
using ArcCore.UI.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ArcCore.UI.SongSelection
{
    public class DifficultyListDisplay : MonoBehaviour
    {
        // display objects
        [SerializeField] private List<DifficultyItem> subItems;
        [SerializeField] private DifficultyItem mainItem;
        [SerializeField] private Text currentDiffName;
        public DifficultyGroup NextGroup { get; private set; }

        public void Display(List<Chart> charts, DifficultyGroup selectedDiff, bool playAnimation = false)
        {
            charts.OrderBy(chart => chart.DifficultyGroup.Precedence);
            int diffIndex = 0;
            for (int i = 0; i < charts.Count; i++)
            {
                if (charts[i].DifficultyGroup == selectedDiff)
                {
                    diffIndex = i;
                    mainItem.Set(charts[i]);
                }
            }
            if (diffIndex == charts.Count - 1)
                NextGroup = charts[0].DifficultyGroup;
            else
                NextGroup = charts[diffIndex + 1].DifficultyGroup;

            int j = diffIndex;
            foreach (DifficultyItem item in subItems)
            {
                j++;
                if (j >= charts.Count) j = 0;
                if (j == diffIndex) break;
                item.Set(charts[j]);
            }

            for (int i = charts.Count; i < subItems.Count; i++)
            {
                subItems[i].Reset();
            }

            currentDiffName.text = selectedDiff.Name;

            //TODO: Animation
        }
    }
}