using System.Collections.Generic;
using UnityEngine;

namespace ArcCore.Gameplay.Behaviours
{
    public class SCManager : MonoBehaviour
    {
        [HideInInspector]
        public BaseSCObject[] objs;

        public void Reset()
        {
            foreach(var obj in objs)
            {
                obj.Reset();
                obj.enabled = false;
            }
        }

        public void Update()
        {
            var time = Conductor.Instance.receptorTime;

            for(int i = 0; i < objs.Length; i++)
            {
                var obj = objs[i];

                bool nval = obj.enabled;
                while(obj.enableKeys.Unfinished && time < obj.enableKeys.Current.timing)
                {
                    nval = obj.enableKeys.Current.value;
                }
                obj.enabled = nval;
            }
        }
    }
}