using System.Collections.Generic;
using UnityEngine;

namespace ArcCore.Gameplay.Behaviours
{
    public class IndicatorManager : MonoBehaviour
    {
        protected List<IIndicator> indicatorList;
        protected List<int> indexByDestroyTime;
        private int indexCache = 0;

        public void Update()
        {
            int currentTime = Conductor.Instance.receptorTime;  

            // If for whatever reason the chart jumped back in time, update indexcache and reenable indicator
            while (indexCache > 0 && currentTime < indicatorList[indexByDestroyTime[indexCache]].endTime)
            {
                indicatorList[indexByDestroyTime[indexCache]].Disable();
                indexCache--;
            }

            // Progress the list and disable anything that needs to be disabled
            while (indexCache < indicatorList.Count && currentTime < indicatorList[indexByDestroyTime[indexCache]].endTime)
            {
                indicatorList[indexByDestroyTime[indexCache]].Disable();
                indexCache++;
            }

            // This is really stupid
            if (currentTime < indicatorList[indexByDestroyTime[0]].endTime)
                indicatorList[indexByDestroyTime[0]].Disable();

            if (currentTime < indicatorList[indexByDestroyTime[indicatorList.Count]].endTime)
                indicatorList[indexByDestroyTime[indicatorList.Count]].Disable();
        }

        public void Initialize(List<IIndicator> indicatorList)
        {
            Destroy();
            this.indicatorList = indicatorList;

            indexByDestroyTime = new List<int>(indicatorList.Count);
            for (int i=0; i<indicatorList.Count; i++) indexByDestroyTime.Add(i);

            indexByDestroyTime.Sort((a, b) => indicatorList[a].endTime.CompareTo(indicatorList[b].endTime));
        }

        public void Destroy()
        {
            foreach (IIndicator indicator in indicatorList)
            {
                indicator.Destroy();
            }
        }

        public IIndicator GetIndicator(int groupId)
            => indicatorList[groupId];
    }
}