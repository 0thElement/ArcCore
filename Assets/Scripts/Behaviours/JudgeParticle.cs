using UnityEngine;
using Unity.Mathematics;

namespace ArcCore.Behaviours
{
    public class ScreenSprite : MonoBehaviour
    {
        public SpriteRenderer spriteRenderer;
        public float2 screenPos;

        public void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            SafeAwake();
        }

        public new virtual void SafeAwake() {}

        public void LateUpdate()
        {
            Camera c = Camera.main;
            transform.forward = c.transform.forward;
            transform.position = c.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, c.nearClipPlane + 0.01f));
        }
    }
    public class JudgeParticle : ScreenSprite
    {
        public const int lifetimeMax = 30;
        [HideInInspector] public int lifetime = lifetimeMax + 1;

        public override void SafeAwake()
        {
            var clr = spriteRenderer.color;
            clr.a = 0;
            spriteRenderer.color = clr;
        }

        public void Update()
        {
            lifetime--;
            if (lifetime < 0) Destroy(gameObject);

            screenPos.y += lifetime * lifetime / 180f;

            var clr = spriteRenderer.color;
            clr.a = 1f - (float)lifetime / lifetimeMax;
            spriteRenderer.color = clr;
        }
    }
}