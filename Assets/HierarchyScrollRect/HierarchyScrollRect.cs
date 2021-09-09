using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using SG;

namespace Zeroth.HierarchyScroll
{
    public class HierarchyScrollRect : ScrollRect
    {
        //OBJECT POOL
        [SerializeField] private int CellPoolSize = 10;
        [SerializeField] private float RecyclingThreshold = 0.2f;
        private ResourceManager resourceManager => ResourceManager.Instance;

        //DATA SOURCES
        ///<summary>
        ///User defined cell data
        ///</summary>
        private List<CellDataBase> dataSource;
        ///<summary>
        ///Store hierarchy data necessary for rendering. Should be a 1-1 correspondance with dataSource
        ///</summary>
        private List<HierarchyCellData> hierarchy;

        //SCROLL MANAGEMENT
        ///<summary>
        ///Stores real visible cells in the scene
        ///</summary>
        private List<RectTransform> activeCells;

        ///<summary>
        ///Point to index of the true cell in the hierarchy that the top active cell correspond to
        ///</summary>
        private int topmostActiveCellIndex = 0;
        ///<summary>
        ///Point to index of the true cell in the hierarchy that the bottom active cell correspond to
        ///</summary>
        private int bottommostAcitveCellIndex = 0;

        ///<summary>
        ///Base size of content recttransform as set in the editor
        ///</summary>
        private float baseContentSize;
        ///<summary>
        ///Previous position of the scroll rect. Used to calculate scrolling direction
        ///</summary>
        private Vector2 prevAnchoredPos;

        ///<summary>
        ///Whether some functions are still performing recycling action. Used to prevent unwanted function calls
        ///</summary>
        private bool isRecycling = false;

        //Bounding y value
        private float minY;
        private float maxY;

        //2-stage loading
        [SerializeField] private float LoadCellsFullyMaxScrollVelocity = 50f;

        #region Data
        ///<summary>
        ///Add cell to cells list along with their children
        ///</summary>
        private void AddCell(CellDataBase cellData, int parent = -1, int indent = 0)
        {
            int index = dataSource.Count;

            dataSource.Add(cellData);
            hierarchy.Add(new HierarchyCellData{
                prefabName = cellData.prefab.name,
                isCollapsed = false,
                parentIndex = parent,
                indentDepth = indent,
                indexFlat = index,
                isFullyLoaded = false
            });

            resourceManager.InitPool(cellData.prefab, CellPoolSize);

            if (cellData.children != null)
            {
                foreach (CellDataBase child in cellData.children)
                {
                    AddCell(child, index, indent + 1);
                }
            }
        }

        ///<summary>
        ///Reset the scroll rect with new data
        ///<param name="data">The list of parent cells. Each cell may or may not have children cells</param>
        ///</summary>
        public void SetData(List<CellDataBase> data)
        {
            dataSource = new List<CellDataBase>();
            hierarchy = new List<HierarchyCellData>();
            foreach (CellDataBase cellData in data)
            {
                AddCell(cellData);
            }
            Refresh(true);
            Intitialize();
        }

        private void Intitialize()
        {
            SetTopAnchor(content);
            prevAnchoredPos = content.anchoredPosition;
            baseContentSize = content.sizeDelta.y;
            vertical = true;
            horizontal = false;

            onValueChanged.RemoveListener(OnValueChangedListener);
            onValueChanged.AddListener(OnValueChangedListener);

        }

        ///<summary>
        ///Check if a cell is visible in the hierarchy
        ///A cell can only be visible if any of its parent is collapsed
        ///</summary>
        private bool IsVisible(int index)
        {
            HierarchyCellData cell = hierarchy[index];
            if (cell.parentIndex == -1) return true;

            HierarchyCellData parent = hierarchy[cell.parentIndex];
            if (parent.isCollapsed) return false;

            return IsVisible(cell.parentIndex);
        }
        #endregion


        #region Scrolling
        ///<summary>
        ///Update on scrolling
        ///</summary>
        private void OnValueChangedListener(Vector2 normalizedPos)
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
        public void Refresh(bool backToTop = false)
        {
            //Reset scrollrect
            StopMovement();

            float baseY = 0;
            isRecycling = true;

            if (backToTop)
            {
                topmostActiveCellIndex = 0;
                m_ContentStartPosition = Vector2.zero;
                prevAnchoredPos = Vector2.zero;
                content.anchoredPosition = Vector2.zero;
            }

            if (activeCells != null && activeCells.Count > 0 && !backToTop)
                baseY = topCell.anchoredPosition.y;

            //Clean list
            if (activeCells != null)
                activeCells.ForEach((RectTransform cell) => ReturnObjectToPool(cell));

            activeCells = new List<RectTransform>();
            
            //Reset index
            topmostActiveCellIndex = Math.Min(topmostActiveCellIndex, hierarchy.Count - 1);
            bottommostAcitveCellIndex = topmostActiveCellIndex;

            //Repopulate list with new cells
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
            while (bottommostAcitveCellIndex < hierarchy.Count && bottomCell.MinY() > minY);
            bottommostAcitveCellIndex--;

            //Nudge cells because there might be gaps
            NudgeToBottom(out Vector2 _);
            NudgeToTop(out Vector2 _);

            //If there's not enough cells to fill the viewport then cut short content's size so user can't scroll
            SetBound();
            float newContentSize = (totalCellsHeight < maxY - minY) ? (maxY - minY) : baseContentSize;
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
        ///On scrolling down
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
        #endregion

        #region CellInteraction
        ///<summary>
        ///Collapse a cell. Any children cell will be hidden from view
        ///</summary>
        public void ToggleCollapse(int cellIndex)
        {
            hierarchy[cellIndex].isCollapsed = !(hierarchy[cellIndex].isCollapsed);
            Refresh();
        }

        ///<summary>
        ///Call all cells to take action when the viewport scroll slow enough
        ///Useful for example for loading texture in cells which is slow
        ///</summary>
        private void LoadCellsFully()
        {
            if (activeCells == null) return;
            foreach (RectTransform cell in activeCells)
            {
                HierarchyCellData cellData = cell.GetComponent<CellBase>().hierarchyData;
                if (cellData.isFullyLoaded) continue;

                CellDataBase data = dataSource[cellData.indexFlat];
                cell.GetComponent<CellBase>().SetCellDataFully(data);
                cellData.isFullyLoaded = true;
            }
        }

        #endregion

        #region Helper
        /// <summary>
        /// Anchoring cell and content rect transforms to top preset. Makes repositioning easy.
        /// </summary>
        /// <param name="rectTransform"></param>
        private void SetTopAnchor(RectTransform rectTransform)
        {
            //Saving to reapply after anchoring. Width and height changes if anchoring is change. 
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;

            //Setting top anchor 
            rectTransform.anchorMin = new Vector2(0.5f, 1);
            rectTransform.anchorMax = new Vector2(0.5f, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);

            //Reapply size
            rectTransform.sizeDelta = new Vector2(width, height);
        }

        ///<summary>
        ///Create a cell from object pool and initialize it with the corresponding data from <see cref="dataSource">
        ///</summary>
        private RectTransform InitializeCellFromPool(int i)
        {
            GameObject obj = resourceManager.GetObjectFromPool(hierarchy[i].prefabName);
            CellBase cell = obj.GetComponent<CellBase>();

            cell.hierarchyData = hierarchy[i];
            cell.hierarchyData.isFullyLoaded = false;
            cell.scrollRect = this;
            cell.SetCellData(dataSource[i]);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(content, false);
            SetTopAnchor(rect);

            return rect;
        }

        private void ReturnObjectToPool(RectTransform rect)
        {
            CellBase cell = rect.GetComponent<CellBase>();
            cell.ClearCell();

            resourceManager.ReturnObjectToPool(rect.gameObject);
        }

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
        #endregion
    }
}