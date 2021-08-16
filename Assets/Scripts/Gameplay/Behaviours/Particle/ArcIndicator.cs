using UnityEngine;
using Unity.Mathematics;

namespace ArcCore.Gameplay.Behaviours
{
    public class ArcIndicator : IIndicator
    {
        public int startTime {get; set;}
        public int endTime {get; set;}

        private ParticleSystem particle;
        private SpriteRenderer diamond;
        private Transform transform;

        public ArcIndicator(GameObject gameObject, int startTime, int endTime)
        {
            this.startTime = startTime;
            this.endTime = endTime;

            particle = gameObject.GetComponent<ParticleSystem>();
            diamond = gameObject.GetComponent<SpriteRenderer>();
            transform = gameObject.GetComponent<Transform>();
        }

        public void Enable()
        {
            transform.gameObject.SetActive(true);
        }

        public void Disable()
        {
            particle.Clear();
            particle.Stop();
            transform.gameObject.SetActive(false);
        }

        public void Update(float3 position)
        {
            Enable();
            float dist = position.z;

            position.z = 0;
            transform.localPosition = position;

            //to be changed
            float diamondScale = dist;
            transform.localScale = new float3(diamondScale, diamondScale, 1);
        }

        public void PlayParticle()
        {
            particle.Play();
        }
    }
}