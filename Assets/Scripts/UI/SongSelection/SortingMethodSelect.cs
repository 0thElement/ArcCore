using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Zeroth.HierarchyScroll;
using ArcCore.Storage.Data;

namespace ArcCore.UI.SongSelection
{
    public class SortingMethodSelect : MonoBehaviour
    {
        public static GroupLevelMethod Method { get; private set; }

        private static GroupLevelMethod[] groupMethods = new GroupLevelMethod[]
        {
            new NoGroupingMethod(),
            new GroupByDifficultyDisplayMethod(),
        };

        private static SortLevelMethod[] sortMethods = new SortLevelMethod[]
        {
            new SortByDifficultyMethod(),
            new SortByTitleMethod(),
        };

        private static int currentGroupMethodIndex = 0;
        private static GroupLevelMethod currentGroupMethod => groupMethods[currentGroupMethodIndex];
        [SerializeField] private Text groupMethodText;

        private static int currentSortMethodIndex = 0;
        private static SortLevelMethod currentSortMethod => sortMethods[currentSortMethodIndex];
        [SerializeField] private Text sortMethodText;

        public static List<CellDataBase> Convert(List<Level> levels, DifficultyGroup diff, GameObject cellPrefab, GameObject folderPrefab)
        {
            return currentGroupMethod.Convert(levels, cellPrefab, diff, currentSortMethod, folderPrefab);
        }

        private void Awake()
        {
            groupMethodText.text = currentGroupMethod.MethodName;
            sortMethodText.text = currentSortMethod.MethodName;
        }

        public void CycleGroupMethod()
        {
            if (++currentGroupMethodIndex >= groupMethods.Length) currentGroupMethodIndex = 0;
            groupMethodText.text = currentGroupMethod.MethodName;
            SelectionMenu.Instance.DrawLevels();
        }

        public void CycleSortMethod()
        {
            if (++currentSortMethodIndex >= sortMethods.Length) currentSortMethodIndex = 0;
            sortMethodText.text = currentSortMethod.MethodName;
            SelectionMenu.Instance.DrawLevels();
        }
    }
}