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
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private GameObject diffPrefab;

        public void Display(List<Chart> charts, DifficultyGroup selectedDiff, bool playAnimation = false)
        {
            //read scrollrect's pos value and determine the correct diff to display
            //each diff cell's appearance is changed depending on its position
            //also the color should change gradually
            //if playanimation is true, the cell's position will be animated
        }
    }
}