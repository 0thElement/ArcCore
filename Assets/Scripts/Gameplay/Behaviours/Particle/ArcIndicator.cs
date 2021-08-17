using UnityEngine;
using Unity.Mathematics;

namespace ArcCore.Gameplay.Behaviours
{
    public class ArcIndicator : IIndicator
    {
        public int endTime {get; set;}

        private ParticleSystem particle;
        private SpriteRenderer diamond;
        private Transform transform;

        public ArcIndicator(GameObject gameObject, int endTime)
        {
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
            StopParticle();
            transform.gameObject.SetActive(false);
        }

        public void Update(float3 position)
        {
            Enable();
            float dist = Mathf.Abs(position.z) / 100000;

            float scale = 0.35f + 0.5f * (1 - dist);
            transform.localScale = new float3(scale, scale, 1);

            diamond.color = new Color(1, 1, 1, dist);

            position.z = 0;
            transform.localPosition = position;
        }

        public void PlayParticle()
        {
            particle.Play();
        }
        public void StopParticle()
        {
            particle.Clear();
            particle.Stop();
        }

        public void Destroy()
        {
            Object.Destroy(transform.gameObject);
        }
    }
}