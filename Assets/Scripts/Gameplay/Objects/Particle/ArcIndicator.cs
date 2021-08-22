using UnityEngine;
using Unity.Mathematics;

namespace ArcCore.Gameplay.Behaviours
{
    public class ArcIndicator : IIndicator
    {
        public int EndTime {get; set;}

        private ParticleSystem particle;
        private SpriteRenderer diamond;
        private Transform particleTransform;
        private Transform diamondTransform;

        public ArcIndicator(GameObject diamondObj, GameObject particleObj, int endTime)
        {
            this.EndTime = endTime;
            diamondObj.SetActive(false);
            particleObj.SetActive(false);

            particle = particleObj.GetComponent<ParticleSystem>();
            diamond = diamondObj.GetComponent<SpriteRenderer>();
            particleTransform = particleObj.GetComponent<Transform>();
            diamondTransform = diamondObj.GetComponent<Transform>();
        }

        public void Enable()
        {
            particleTransform.gameObject.SetActive(true);
            diamondTransform.gameObject.SetActive(true);
        }

        public void Disable()
        {
            StopParticle();
            particleTransform.gameObject.SetActive(false);
            diamondTransform.gameObject.SetActive(false);
        }

        public void Update(float3 position)
        {
            Enable();
            float dist = Mathf.Abs(position.z) / 100;

            float scale = 0.35f + 0.5f * dist;
            diamondTransform.localScale = new float3(scale, scale, 1);

            diamond.color = new Color(1, 1, 1, 1 - dist);

            position.z = 0;
            diamondTransform.localPosition = position;
            particleTransform.localPosition = position;
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

        public float2 GetPosition()
        {
            return new float2(diamondTransform.position.x, diamondTransform.position.y);
        }

        public void Destroy()
        {
            Object.Destroy(diamondTransform.gameObject);
            Object.Destroy(particleTransform.gameObject);
        }
    }
}