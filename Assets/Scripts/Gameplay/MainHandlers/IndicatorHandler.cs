using System.Collections.Generic;
using UnityEngine;

namespace ArcCore.Gameplay.Objects.Particle
{
    public class IndicatorHandler
    {
        //A class for managing approach indicators of arc and traces
        
        /// <summary>
        /// List of indicators managed by this object
        /// </summary>
        protected List<IIndicator> indicatorList;

        /// <sumamry>
        /// Indexing indicator list by indicator's destroy time, used to automatically kill indicators when needed to
        /// </summary>
        protected List<int> indexByDestroyTime;

        /// <sumamry>
        /// Caching the last index accessed, minimizes index access
        /// </summary>
        public int indexCache = 0;

        /// <sumamry>
        /// If the manager is currently managing a list of indicators
        /// </summary>
        private bool isInitialized = false;

        public void CheckForDisable()
        {
            if (!isInitialized) return;

            int currentTime = PlayManager.ReceptorTime;  

            // If for whatever reason the chart jumped back in time, update indexcache and reenable indicator
            while (indexCache > 0 && currentTime < indicatorList[indexByDestroyTime[indexCache-1]].EndTime)
            {
                indicatorList[indexByDestroyTime[indexCache]].Disable();
                indexCache--;
            }

            // Progress the list and disable anything that needs to be disabled
            while (indexCache < indicatorList.Count - 1 && currentTime >= indicatorList[indexByDestroyTime[indexCache]].EndTime)
            {
                indicatorList[indexByDestroyTime[indexCache]].Disable();
                indexCache++;
            }

            // This is really stupid
            if (currentTime > indicatorList[indexByDestroyTime[0]].EndTime)
                indicatorList[indexByDestroyTime[0]].Disable();

            if (currentTime > indicatorList[indexByDestroyTime[indicatorList.Count - 1]].EndTime)
                indicatorList[indexByDestroyTime[indicatorList.Count - 1]].Disable();
        }

        /// <summary>
        /// Initializes the manager with indicators
        /// </summary>
        /// TODO: instead of constructing indicator outside of this class, initializes them here
        /// Will be done when refactor ArcEntityCreator and TraceEntityCreator
        public void Initialize(List<IIndicator> indicatorList)
        {
            Destroy();
            this.indicatorList = indicatorList;

            indexByDestroyTime = new List<int>(indicatorList.Count);
            for (int i=0; i<indicatorList.Count; i++) indexByDestroyTime.Add(i);

            indexByDestroyTime.Sort((a, b) => indicatorList[a].EndTime.CompareTo(indicatorList[b].EndTime));

            if (indicatorList.Count > 0) isInitialized = true;
        }

        /// <summary>
        /// Destroys all indicators managed under this class
        /// </summary>
        public void Destroy()
        {
            if (!isInitialized) return;
            foreach (IIndicator indicator in indicatorList)
            {
                indicator.Destroy();
            }
            isInitialized = false;
        }

        /// <summary>
        /// Provides an interface for other gameplay class to control indicators (i.e positions, particles' activeness)
        /// </summary>
        public IIndicator GetIndicator(int groupId)
            => indicatorList[groupId];
    }
}