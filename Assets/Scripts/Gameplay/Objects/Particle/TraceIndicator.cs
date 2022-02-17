using UnityEngine;
using Unity.Mathematics;

namespace ArcCore.Gameplay.Objects.Particle
{
    public class TraceIndicator : IIndicator
    {
        public int EndTime {get; set;}
        private bool enabled = false;

        private Transform transform;

        public TraceIndicator(GameObject gameObject, int endTime)
        {
            this.EndTime = endTime;
            gameObject.SetActive(false);
            transform = gameObject.GetComponent<Transform>();
        }

        public void Enable()
        {
            if (enabled) return;
            enabled = true;
            transform.gameObject.SetActive(true);
        }

        public void Disable()
        {
            enabled = false;
            transform.gameObject.SetActive(false);
        }

        public void Update(float3 position)
        {
            if (PlayManager.ReceptorTime > EndTime) return;

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