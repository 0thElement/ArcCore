using UnityEngine;

namespace ArcCore.Behaviours
{
    public class JudgeParticle : MonoBehaviour
    {
        public const int lifetimeMax = 30;

        [HideInInspector] public int lifetime = lifetimeMax + 1;
        [HideInInspector] public SpriteRenderer spriteRenderer;

        public void Awake()
        {
            lifetime = 30;
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 0);
        }

        public void Update()
        {
            lifetime--;
            if (lifetime < 0) Destroy(gameObject);

            transform.position = new Vector3(transform.position.x, transform.position.y + lifetime * lifetime / 6000f);
            transform.forward = Camera.main.transform.forward;
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 1f - (float)lifetime / lifetimeMax);
        }
    }
}