using UnityEngine;
using System;
using System.Collections.Generic;

namespace Zeroth.HierarchyScroll
{
    public class HorizontalHierarchyScrollRect : HierarchyScrollRect
    {
        ///<summary>
        ///Point to index of the true cell in the hierarchy that the top active cell correspond to
        ///</summary>
        private int leftmostActiveCellIndex = 0;
        ///<summary>
        ///Point to index of the true cell in the hierarchy that the bottom active cell correspond to
        ///</summary>
        private int rightmostActiveCellIndex = 0;

        ///<summary>
        ///Whether some functions are still performing recycling action. Used to prevent unwanted function calls
        ///</summary>
        private bool isRecycling = false;

        //Bounding y value
        private float minX;
        private float maxX;

        #region Scrolling
        protected override void SetupScrollDirection()
        {
            vertical = false;
            horizontal = true;
        }

        ///<summary>
        ///Update on scrolling
        ///</summary>
        protected override void OnValueChangedListener(Vector2 normalizedPos)
        {
            Vector2 dir = content.anchoredPosition - prevAnchoredPos;
            m_ContentStartPosition += Scroll(dir);
            prevAnchoredPos = content.anchoredPosition;

            if (Mathf.Abs(dir.x) <= LoadCellsFullyMaxScrollVelocity) LoadCellsFully();
        }

        ///<summary>
        ///Refresh the active cells. Call this every time there's structural change to the hierarchy
        ///such as hiding cells
        ///</summary>
        protected override void Refresh(bool backToStart = false)
        {
            //Reset scrollrect
            StopMovement();

            float baseX = 0;
            isRecycling = true;

            if (backToStart)
            {
                leftmostActiveCellIndex = 0;
                m_ContentStartPosition = Vector2.zero;
                prevAnchoredPos = Vector2.zero;
                content.anchoredPosition = Vector2.zero;
            }

            if (activeCells != null && activeCells.Count > 0 && !backToStart)
                baseX = leftCell.anchoredPosition.x;

            //Clean list
            if (activeCells != null)
                activeCells.ForEach((RectTransform cell) => ReturnObjectToPool(cell));

            activeCells = new List<RectTransform>();
            
            //Reset index
            leftmostActiveCellIndex = Math.Min(leftmostActiveCellIndex, hierarchy.Count - 1);
            rightmostActiveCellIndex = leftmostActiveCellIndex;

            //Repopulate list with new cells
            SetBound();
            float totalCellsWidth = 0;
            do
            {
                if (IsVisible(rightmostActiveCellIndex))
                {
                    RectTransform obj = InitializeCellFromPool(rightmostActiveCellIndex);
                    obj.anchoredPosition = new Vector2(baseX, 0);

                    activeCells.Add(obj);

                    baseX += obj.sizeDelta.x;
                    totalCellsWidth += obj.sizeDelta.x;
                }
                rightmostActiveCellIndex++;
            }
            while (rightmostActiveCellIndex < hierarchy.Count && totalCellsWidth < maxX - minX);
            rightmostActiveCellIndex--;

            //If not enough cells from the bottom direction are there to fill the area then fill from the top
            Vector2 offset = Vector2.zero;
            while (leftmostActiveCellIndex - 1 >= 0 && totalCellsWidth < maxX - minX)
            {
                leftmostActiveCellIndex--;
                if (IsVisible(leftmostActiveCellIndex))
                {
                    RectTransform obj = InitializeCellFromPool(leftmostActiveCellIndex);
                    obj.anchoredPosition = new Vector2(leftCell.anchoredPosition.x - leftCell.sizeDelta.x, 0);

                    activeCells.Insert(0, obj);
                    
                    offset.x += obj.sizeDelta.x;
                    totalCellsWidth += obj.sizeDelta.x;
                }
            }
            activeCells.ForEach((RectTransform cell) => cell.anchoredPosition += offset);
            content.anchoredPosition -= offset;

            //Nudge cells because there might be gaps
            NudgeToRight(out Vector2 _);
            NudgeToLeft(out Vector2 _);

            //If there's not enough cells to fill the viewport then cut short content's size so user can't scroll
            float newContentSize = (totalCellsWidth < maxX - minX) ? ((totalCellsWidth < baseContentSize.x) ? baseContentSize.x : totalCellsWidth): totalCellsWidth;
            content.sizeDelta = new Vector2(newContentSize, content.sizeDelta.y);
            
            LoadCellsFully();
            UpdateAllCellSlant();
            isRecycling = false;
        }

        ///<summary>
        ///Scroll the cells and container. Call <see cref="ScrollRight"> or <see cref="ScrollLeft"> based on the direction of scrolling
        ///<param name="dir">Scroll direction</param>
        ///<returns>An offset vector corresponding to how far content has been offseted</returns>
        ///</summary>
        private Vector2 Scroll(Vector2 dir)
        {
            if (isRecycling) return Vector2.zero;
            SetBound();
            if (dir.x > 0) return ScrollLeft();
            if (dir.x < 0) return ScrollRight();
            return Vector2.zero;
        }

        //Shorthands
        private RectTransform leftCell => activeCells[0];
        private RectTransform rightCell => activeCells[activeCells.Count - 1];

        ///<summary>
        ///Move all active cells to close empty gap at right of content
        ///Stores nudge offset in the nudge parameter
        ///<returns>True if nudging was performed, false otherwise</returns>
        ///</summary>
        private bool NudgeToRight(out Vector2 nudge)
        {
            nudge = Vector2.zero;

            //Only nudge in one direction at a time or we will be thrown into a loop
            if (rightmostActiveCellIndex == hierarchy.Count - 1 && leftmostActiveCellIndex != 0)
            {
                Vector2 offset = new Vector2(rightCell.MaxX() - content.MaxX(), 0);

                activeCells.ForEach((RectTransform cell) => cell.anchoredPosition -= offset);
                content.anchoredPosition += offset;

                nudge = offset;
                return true;
            }
            return false;
        }

        ///<summary>
        ///Move all active cells up to close empty gap at left of content if reached the last cell in the hierarchy
        ///Stores nudge offset in the nudge parameter
        ///<returns>True if nudging was performed, false otherwise</returns>
        ///</summary>
        private bool NudgeToLeft(out Vector2 nudge)
        {
            nudge = Vector2.zero;

            if (leftmostActiveCellIndex == 0)
            {
                Vector2 offset = new Vector2(leftCell.MinX() - content.MinX(), 0);

                activeCells.ForEach((RectTransform cell) => cell.anchoredPosition -= offset);
                content.anchoredPosition += offset;

                nudge = offset;
                return true;
            }
            return false;
        }

        ///<summary>
        ///On scrolling right
        ///<returns>An offset vector corresponding to how far content has been offseted</returns>
        ///</summary>
        private Vector2 ScrollRight()
        {
            if (NudgeToRight(out Vector2 nudge)) return nudge;

            Vector2 offset = Vector2.zero;
            isRecycling = true;

            //Kill cells that get outside left range
            while (activeCells.Count > 0 && leftCell.MaxX() < minX)
            {
                offset.x += leftCell.sizeDelta.x;
                ReturnObjectToPool(leftCell);
                activeCells.RemoveAt(0);

                leftmostActiveCellIndex = leftCell.GetComponent<CellBase>().hierarchyData.indexFlat;
            }

            //Add cells to fill in remaining space on the right
            while (rightmostActiveCellIndex < hierarchy.Count - 1 && rightCell.MaxX() < maxX)
            {
                rightmostActiveCellIndex++;

                if (IsVisible(rightmostActiveCellIndex)){
                    RectTransform newCell = InitializeCellFromPool(rightmostActiveCellIndex);
                    newCell.anchoredPosition = new Vector2(rightCell.anchoredPosition.x + rightCell.sizeDelta.x, rightCell.anchoredPosition.y);

                    activeCells.Add(newCell);
                }
            }

            activeCells.ForEach((RectTransform cell) => cell.anchoredPosition -= offset);
            content.anchoredPosition += offset;
            isRecycling = false;
            return offset;
        }

        ///<summary>
        ///On scrolling left
        ///<returns>An offset vector corresponding to how far content has been offseted</returns>
        ///</summary>
        private Vector2 ScrollLeft()
        {
            if (NudgeToLeft(out Vector2 nudge)) return nudge;

            Vector2 offset = Vector2.zero;
            isRecycling = true;

            //Kill cells that get outside right range
            while (activeCells.Count > 0 && rightCell.MinX() > maxX)
            {
                offset.x += rightCell.sizeDelta.x;
                ReturnObjectToPool(rightCell);
                activeCells.RemoveAt(activeCells.Count - 1);

                rightmostActiveCellIndex = rightCell.GetComponent<CellBase>().hierarchyData.indexFlat;
            }

            //Add cells to fill in remaining space at the left
            while (leftmostActiveCellIndex > 0 && leftCell.MinX() > minX)
            {
                leftmostActiveCellIndex--;

                if (IsVisible(leftmostActiveCellIndex))
                {
                    RectTransform newCell = InitializeCellFromPool(leftmostActiveCellIndex);
                    newCell.anchoredPosition = new Vector2(leftCell.anchoredPosition.x - newCell.sizeDelta.x, leftCell.anchoredPosition.y);

                    activeCells.Insert(0, newCell);
                }
            }

            activeCells.ForEach((RectTransform cell) => cell.anchoredPosition += offset);
            content.anchoredPosition -= offset;
            isRecycling = false;
            return -offset;
        }
        #endregion

        ///<summary>
        ///Move to the index-th cell in the hierarchy. Also force the cell to be expanded.
        ///</summary>
        public override void Jump(int index)
        {
            index = Math.Min(index, hierarchy.Count - 1);
            index = Math.Max(index, 0);

            ForceVisible(index);

            //Assume index-th cell at middle, calculate the top cell index. Then set the top index and refresh
            SetBound();
            float halfHeight = (maxX - minX) / 2;
            float totalCellsHeight = 0;

            while (totalCellsHeight < halfHeight && index - 1 >= 0)
            {
                index--;
                if (IsVisible(index))
                {
                    RectTransform obj = InitializeCellFromPool(index);
                    totalCellsHeight += obj.sizeDelta.x;
                    ReturnObjectToPool(obj);
                }
            }

            leftmostActiveCellIndex = index;
            Refresh();
        }

        #region Helper
        ///<summary>
        ///Recalculate the bounding y values <see cref="minX"> and <see cref="maxX">. Should be called often in case there's a resolution change
        ///</summary>
        private void SetBound()
        {
            Vector3[] corners = viewport.GetCorners();
            float threshold = RecyclingThreshold * (corners[2].x - corners[0].x);
            minX = corners[0].x - threshold;
            maxX = corners[2].x + threshold;
        }

        protected override void SetTopAnchor(RectTransform rectTransform)
        {
            rectTransform.anchorMin = new Vector2(0, rectTransform.anchorMin.y);
            rectTransform.anchorMax = new Vector2(0, rectTransform.anchorMax.y);
            rectTransform.pivot = new Vector2(0, rectTransform.pivot.y);
        }

        protected override void UpdateCellSlant(RectTransform rectTransform)
        {
            Vector3 pos = rectTransform.TransformPoint(Vector3.zero) - viewport.TransformPoint(Vector3.zero);
            float y = Mathf.Tan(SlantAngle) * pos.x;
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, y);
        }
        #endregion
    }
}