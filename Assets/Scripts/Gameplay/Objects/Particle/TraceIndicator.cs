using UnityEngine;
using Unity.Mathematics;

namespace ArcCore.Gameplay.Objects.Particle
{
    public class TraceIndicator : IIndicator
    {
        public int EndTime {get; set;}

        private Transform transform;

        public TraceIndicator(GameObject gameObject, int endTime)
        {
            this.EndTime = endTime;
            gameObject.SetActive(false);
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
        
        public void Destroy()
        {
            Object.Destroy(transform.gameObject);
        }
    }
}