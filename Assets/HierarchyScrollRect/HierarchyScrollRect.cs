using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using SG;

namespace Zeroth.HierarchyScroll
{
    public abstract class HierarchyScrollRect : ScrollRect
    {
        //OBJECT POOL
        [SerializeField] protected int CellPoolSize = 10;
        [SerializeField] protected float RecyclingThreshold = 1f;
        [SerializeField] protected float SlantAngle = 10;
        private ResourceManager resourceManager => ResourceManager.Instance;

        //DATA SOURCES
        ///<summary>
        ///User defined cell data
        ///</summary>
        protected List<CellDataBase> dataSource;
        ///<summary>
        ///Store hierarchy data necessary for rendering. Should be a 1-1 correspondance with dataSource
        ///</summary>
        protected List<HierarchyCellData> hierarchy;

        //SCROLL MANAGEMENT
        ///<summary>
        ///Stores real visible cells in the scene
        ///</summary>
        protected List<RectTransform> activeCells;
        ///<summary>
        ///Base size of content recttransform as set in the editor
        ///</summary>
        protected Vector2 baseContentSize;
        ///<summary>
        ///Previous position of the scroll rect. Used to calculate scrolling direction
        ///</summary>
        protected Vector2 prevAnchoredPos;

        //2-stage loading
        [SerializeField] protected float LoadCellsFullyMaxScrollVelocity = 50f;

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
        public void SetData(List<CellDataBase> data, bool backToStart = false)
        {
            dataSource = new List<CellDataBase>();
            hierarchy = new List<HierarchyCellData>();
            foreach (CellDataBase cellData in data)
            {
                AddCell(cellData);
            }
            Intitialize();
            Refresh(backToStart);
        }

        private void Intitialize()
        {
            SetTopAnchor(content);
            prevAnchoredPos = content.anchoredPosition;
            baseContentSize = content.sizeDelta;
            SetupScrollDirection();

            onValueChanged.RemoveListener(OnValueChangedListener);
            onValueChanged.AddListener(OnValueChangedListener);

        }

        ///<summary>
        ///Check if a cell is visible in the hierarchy
        ///A cell can only be visible if any of its parent is collapsed
        ///</summary>
        protected bool IsVisible(int index)
        {
            HierarchyCellData cell = hierarchy[index];
            if (cell.parentIndex == -1) return true;

            HierarchyCellData parent = hierarchy[cell.parentIndex];
            if (parent.isCollapsed) return false;

            return IsVisible(cell.parentIndex);
        }

        ///<summary>
        ///Force a cell to be visible by expanding all parent cells
        ///</summary>
        protected void ForceVisible(int index)
        {
            HierarchyCellData cell = hierarchy[index];
            if (cell.parentIndex == -1) return;

            HierarchyCellData parent = hierarchy[cell.parentIndex];
            parent.isCollapsed = false;
            ForceVisible(cell.parentIndex);
        }
        #endregion


        #region Scrolling
        protected abstract void SetupScrollDirection();
        protected abstract void OnValueChangedListener(Vector2 normalizedPos);
        protected abstract void Refresh(bool backToStart = false);
        public abstract void Jump(int index);
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
        protected void LoadCellsFully()
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
        protected abstract void SetTopAnchor(RectTransform rectTransform);

        ///<summary>
        ///Create a cell from object pool and initialize it with the corresponding data from <see cref="dataSource">
        ///</summary>
        protected RectTransform InitializeCellFromPool(int i)
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

        protected void ReturnObjectToPool(RectTransform rect)
        {
            CellBase cell = rect.GetComponent<CellBase>();
            cell.ClearCell();

            resourceManager.ReturnObjectToPool(rect.gameObject);
        }
        #endregion

        #region Slant
        private void Update()
        {
            UpdateAllCellSlant();
        }
        protected void UpdateAllCellSlant()
        {
            if (activeCells == null) return;
            foreach (RectTransform cell in activeCells)
            {
                UpdateCellSlant(cell);
            }
        }
        protected abstract void UpdateCellSlant(RectTransform rectTransform);
        #endregion
    }
}