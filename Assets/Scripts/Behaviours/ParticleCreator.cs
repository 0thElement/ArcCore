using Unity.Mathematics;
using UnityEngine;

namespace ArcCore.Behaviours
{
    public class ParticleCreator : MonoBehaviour
    {
        public enum TextParticleType
        {
            LostJudgeType,
            PureJudgeType,
            FarJudgeType
        }

        public static ParticleCreator Instance { get; private set; }

        [SerializeField] private int tapJudgePoolSize;
        [SerializeField] private int arcJudgePoolSize;
        [SerializeField] private GameObject lostJudgeBase;
        [SerializeField] private GameObject pureJudgeBase;
        [SerializeField] private GameObject farJudgeBase;
        [SerializeField] private GameObject tapEffectBase;
        [SerializeField] private GameObject holdEffectBase;
        [SerializeField] private SpriteRenderer[] laneHighlights;

        private GameObject[] textParticlePool;
        private GameObject[] spriteParticlePool;
        private GameObject[] arcParticlePool;

        private void Awake()
        {
            Instance = this;
            textParticlePool = new GameObject[tapJudgePoolSize];
            spriteParticlePool = new GameObject[tapJudgePoolSize];
            arcParticlePool = new GameObject[arcJudgePoolSize];
        }

        public void PlayParticleAt(float2 position, TextParticleType type)
        {
            
            Debug.Log(position.x + " " + position.y + " " + type);
        }
    }
}