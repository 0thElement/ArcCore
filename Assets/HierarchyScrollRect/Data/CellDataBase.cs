using UnityEngine;
using System.Collections.Generic;

namespace Zeroth.HierarchyScroll
{
    public abstract class CellDataBase
    {
        public GameObject prefab;
        public List<CellDataBase> children;
    }
}