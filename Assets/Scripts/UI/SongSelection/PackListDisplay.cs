using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Zeroth.HierarchyScroll;
using ArcCore.UI.Data;
using System.Linq;

namespace ArcCore.UI.SongSelection
{
    public class PackListDisplay : MonoBehaviour
    {
        [SerializeField] private HierarchyScrollRect scrollRect;
        [SerializeField] private GameObject packPrefab;

        public void Display(List<Pack> packs, List<Level> levels, Pack selectedPack)
        {
            List<CellDataBase> packCells = new List<CellDataBase>();
            foreach (Pack pack in packs)
            {
                IEnumerable<Level> levelsOfPack = levels;
                if (pack != null) levelsOfPack = levels.Where(level => level.Pack != null && level.Pack.Id == pack.Id);
                int count = 0;
                int clear = 0;
                int fr = 0;
                int pm = 0;

                foreach (Level level in levelsOfPack)
                {
                    count += level.Charts.Length;
                    foreach (Chart chart in level.Charts) {
                        switch (chart.PbGrade) {
                            case ScoreCategory.EasyClear:
                            case ScoreCategory.NormalClear:
                            case ScoreCategory.HardClear:
                                clear++;
                                break;
                            case ScoreCategory.FullRecall:
                                fr++;
                                break;
                            case ScoreCategory.PureMemory:
                            case ScoreCategory.Max:
                                pm++;
                                break;
                        }
                    }
                }

                packCells.Add(new PackCellData {
                    prefab = packPrefab,
                    pack = pack,
                    chartCount = count,
                    clearCount = clear,
                    frCount = fr,
                    pmCount = pm
                });
            }
            // scrollRect.SetData(packCells);
        }
    }
}