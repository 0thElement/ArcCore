using UnityEngine;
using System;
using System.Collections.Generic;

namespace Zeroth.HierarchyScroll
{
    public class VerticalHierarchyScrollRect : HierarchyScrollRect
    {
        ///<summary>
        ///Point to index of the true cell in the hierarchy that the top active cell correspond to
        ///</summary>
        private int topmostActiveCellIndex = 0;
        ///<summary>
        ///Point to index of the true cell in the hierarchy that the bottom active cell correspond to
        ///</summary>
        private int bottommostAcitveCellIndex = 0;

        ///<summary>
        ///Whether some functions are still performing recycling action. Used to prevent unwanted function calls
        ///</summary>
        private bool isRecycling = false;

        //Bounding y value
        private float minY;
        private float maxY;

        #region Scrolling
        protected override void SetupScrollDirection()
        {
            vertical = true;
            horizontal = false;
        }
        ///<summary>
        ///Update on scrolling
        ///</summary>
        protected override void OnValueChangedListener(Vector2 normalizedPos)
        {
            Vector2 dir = content.anchoredPosition - prevAnchoredPos;
            m_ContentStartPosition += Scroll(dir);
            prevAnchoredPos = content.anchoredPosition;

            if (Mathf.Abs(dir.y) <= LoadCellsFullyMaxScrollVelocity) LoadCellsFully();
        }

        ///<summary>
        ///Refresh the active cells. Call this every time there's structural change to the hierarchy
        ///such as hiding cells
        ///</summary>
        protected override void Refresh(bool backToStart = false)
        {
            //Reset scrollrect
            StopMovement();

            float baseY = 0;
            isRecycling = true;

            if (backToStart)
            {
                topmostActiveCellIndex = 0;
                m_ContentStartPosition = Vector2.zero;
                prevAnchoredPos = Vector2.zero;
                content.anchoredPosition = Vector2.zero;
            }

            if (activeCells != null && activeCells.Count > 0 && !backToStart)
                baseY = topCell.anchoredPosition.y;

            //Clean list
            if (activeCells != null)
                activeCells.ForEach((RectTransform cell) => ReturnObjectToPool(cell));

            activeCells = new List<RectTransform>();
            
            //Reset index
            topmostActiveCellIndex = Math.Min(topmostActiveCellIndex, hierarchy.Count - 1);
            bottommostAcitveCellIndex = topmostActiveCellIndex;

            //Repopulate list with new cells
            SetBound();
            float totalCellsHeight = 0;
            do
            {
                if (IsVisible(bottommostAcitveCellIndex))
                {
                    RectTransform obj = InitializeCellFromPool(bottommostAcitveCellIndex);
                    obj.anchoredPosition = new Vector2(0, baseY);

                    activeCells.Add(obj);

                    baseY -= obj.sizeDelta.y;
                    totalCellsHeight += obj.sizeDelta.y;
                }
                bottommostAcitveCellIndex++;
            }
            while (bottommostAcitveCellIndex < hierarchy.Count && totalCellsHeight < maxY - minY);
            bottommostAcitveCellIndex--;

            //If not enough cells from the bottom direction are there to fill the area then fill from the top
            Vector2 offset = Vector2.zero;
            while (topmostActiveCellIndex - 1 >= 0 && totalCellsHeight < maxY - minY)
            {
                topmostActiveCellIndex--;
                if (IsVisible(topmostActiveCellIndex))
                {
                    RectTransform obj = InitializeCellFromPool(topmostActiveCellIndex);
                    obj.anchoredPosition = new Vector2(0, topCell.anchoredPosition.y + topCell.sizeDelta.y);

                    activeCells.Insert(0, obj);
                    
                    offset.y += obj.sizeDelta.y;
                    totalCellsHeight += obj.sizeDelta.y;
                }
            }
            activeCells.ForEach((RectTransform cell) => cell.anchoredPosition -= offset);
            content.anchoredPosition += offset;

            //Nudge cells because there might be gaps
            NudgeToBottom(out Vector2 _);
            NudgeToTop(out Vector2 _);

            //If there's not enough cells to fill the viewport then cut short content's size so user can't scroll
            float newContentSize = (totalCellsHeight < maxY - minY) ? (totalCellsHeight < baseContentSize.y ? baseContentSize.y : totalCellsHeight) : totalCellsHeight;
            content.sizeDelta = new Vector2(content.sizeDelta.x, newContentSize);
            
            LoadCellsFully();
            isRecycling = false;
        }

        ///<summary>
        ///Scroll the cells and container. Call <see cref="ScrollDown"> or <see cref="ScrollUp"> based on the direction of scrolling
        ///<param name="dir">Scroll direction</param>
        ///<returns>An offset vector corresponding to how far content has been offseted</returns>
        ///</summary>
        private Vector2 Scroll(Vector2 dir)
        {
            if (isRecycling) return Vector2.zero;
            SetBound();
            if (dir.y > 0) return ScrollDown();
            if (dir.y < 0) return ScrollUp();
            return Vector2.zero;
        }

        //Shorthands
        private RectTransform topCell => activeCells[0];
        private RectTransform bottomCell => activeCells[activeCells.Count - 1];

        ///<summary>
        ///Move all active cells down to close empty gap at bottom of content
        ///Stores nudge offset in the nudge parameter
        ///<returns>True if nudging was performed, false otherwise</returns>
        ///</summary>
        private bool NudgeToBottom(out Vector2 nudge)
        {
            nudge = Vector2.zero;

            //Only nudge in one direction at a time or we will be thrown into a loop
            if (bottommostAcitveCellIndex == hierarchy.Count - 1 && topmostActiveCellIndex != 0)
            {
                Vector2 offset = new Vector2(0, bottomCell.MinY() - content.MinY());

                activeCells.ForEach((RectTransform cell) => cell.anchoredPosition -= offset);
                content.anchoredPosition += offset;

                nudge = offset;
                return true;
            }
            return false;
        }

        ///<summary>
        ///Move all active cells up to close empty gap at top of content if reached the bottommost cell in the hierarchy
        ///Stores nudge offset in the nudge parameter
        ///<returns>True if nudging was performed, false otherwise</returns>
        ///</summary>
        private bool NudgeToTop(out Vector2 nudge)
        {
            nudge = Vector2.zero;

            if (topmostActiveCellIndex == 0)
            {
                Vector2 offset = new Vector2(0, topCell.MaxY() - content.MaxY());

                activeCells.ForEach((RectTransform cell) => cell.anchoredPosition -= offset);
                content.anchoredPosition += offset;

                nudge = offset;
                return true;
            }
            return false;
        }

        ///<summary>
        ///On scrolling down
        ///<returns>An offset vector corresponding to how far content has been offseted</returns>
        ///</summary>
        private Vector2 ScrollDown()
        {
            if (NudgeToBottom(out Vector2 nudge)) return nudge;

            Vector2 offset = Vector2.zero;
            isRecycling = true;

            //Kill cells that get outside top range
            while (activeCells.Count > 0 && topCell.MinY() > maxY)
            {
                offset.y += topCell.sizeDelta.y;
                ReturnObjectToPool(topCell);
                activeCells.RemoveAt(0);

                topmostActiveCellIndex = topCell.GetComponent<CellBase>().hierarchyData.indexFlat;
            }

            //Add cells to fill in remaining space at bottom
            while (bottommostAcitveCellIndex < hierarchy.Count - 1 && bottomCell.MinY() > minY)
            {
                bottommostAcitveCellIndex++;

                if (IsVisible(bottommostAcitveCellIndex)){
                    RectTransform newCell = InitializeCellFromPool(bottommostAcitveCellIndex);
                    newCell.anchoredPosition = new Vector2(bottomCell.anchoredPosition.x, bottomCell.anchoredPosition.y - bottomCell.sizeDelta.y);

                    activeCells.Add(newCell);
                }
            }

            activeCells.ForEach((RectTransform cell) => cell.anchoredPosition += offset);
            content.anchoredPosition -= offset;
            isRecycling = false;
            return -offset;
        }

        ///<summary>
        ///On scrolling up
        ///<returns>An offset vector corresponding to how far content has been offseted</returns>
        ///</summary>
        private Vector2 ScrollUp()
        {
            if (NudgeToTop(out Vector2 nudge)) return nudge;

            Vector2 offset = Vector2.zero;
            isRecycling = true;

            //Kill cells that get outside bottom range
            while (activeCells.Count > 0 && bottomCell.MaxY() < minY)
            {
                offset.y += bottomCell.sizeDelta.y;
                ReturnObjectToPool(bottomCell);
                activeCells.RemoveAt(activeCells.Count - 1);

                bottommostAcitveCellIndex = bottomCell.GetComponent<CellBase>().hierarchyData.indexFlat;
            }

            //Add cells to fill in remaining space at top
            while (topmostActiveCellIndex > 0 && topCell.MaxY() < maxY)
            {
                topmostActiveCellIndex--;

                if (IsVisible(topmostActiveCellIndex))
                {
                    RectTransform newCell = InitializeCellFromPool(topmostActiveCellIndex);
                    newCell.anchoredPosition = new Vector2(topCell.anchoredPosition.x, topCell.anchoredPosition.y + newCell.sizeDelta.y);

                    activeCells.Insert(0, newCell);
                }
            }

            activeCells.ForEach((RectTransform cell) => cell.anchoredPosition -= offset);
            content.anchoredPosition += offset;
            isRecycling = false;
            return offset;
        }

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
            float halfHeight = (maxY - minY) / 2;
            float totalCellsHeight = 0;

            while (totalCellsHeight < halfHeight && index - 1 >= 0)
            {
                index--;
                if (IsVisible(index))
                {
                    RectTransform obj = InitializeCellFromPool(index);
                    totalCellsHeight += obj.sizeDelta.y;
                    ReturnObjectToPool(obj);
                }
            }

            topmostActiveCellIndex = index;
            Refresh();
        }
        #endregion

        #region Helper
        ///<summary>
        ///Recalculate the bounding y values <see cref="minY"> and <see cref="maxY">. Should be called often in case there's a resolution change
        ///</summary>
        private void SetBound()
        {
            Vector3[] corners = viewport.GetCorners();
            float threshold = RecyclingThreshold * (corners[2].y - corners[0].y);
            minY = corners[0].y - threshold;
            maxY = corners[2].y + threshold;
        }

        protected override void SetTopAnchor(RectTransform rectTransform)
        {
            //Saving to reapply after anchoring. Width and height changes if anchoring is change. 
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;

            //Setting top anchor 
            rectTransform.anchorMin = new Vector2(rectTransform.anchorMin.x, 1);
            rectTransform.anchorMax = new Vector2(rectTransform.anchorMin.x, 1);
            rectTransform.pivot = new Vector2(rectTransform.pivot.x, 1);

            //Reapply size
            rectTransform.sizeDelta = new Vector2(width, height);
        }
        #endregion
    }
}