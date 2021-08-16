using UnityEngine;
using Unity.Mathematics;

namespace ArcCore.Gameplay.Behaviours
{
    public class TraceIndicator : IIndicator
    {
        public int startTime {get; set;}
        public int endTime {get; set;}

        private Transform transform;

        public TraceIndicator(GameObject gameObject, int startTime, int endTime)
        {
            this.startTime = startTime;
            this.endTime = endTime;

            transform = gameObject.GetComponent<Transform>();
        }

        public void Enable()
        {
            transform.gameObject.SetActive(true);
        }

        public void Disable()
        {
            transform.gameObject.SetActive(false);
        }

        public void Update(float3 position)
        {
            Enable();

            position.z = 0;
            transform.localPosition = position;
        }
    }
}