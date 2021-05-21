using Unity.Mathematics;
using System.Collections.Generic;
using UnityEngine;

namespace ArcCore.Behaviours
{
    public class ParticleCreator : MonoBehaviour
    {
        public enum JudgeType
        {
            Lost,
            Far,
            Pure
        }
        public enum JudgeDetail
        {
            None,
            Early,
            Late
        }

        public static ParticleCreator Instance { get; private set; }

        [SerializeField] private int textParticlePoolSize;
        [SerializeField] private int tapParticlePoolSize;
        [SerializeField] private int arcParticlePoolSize;

        [SerializeField] private Material[] textJudgeMaterials;

        [SerializeField] private GameObject textParticleBase;
        [SerializeField] private GameObject tapParticleBase;
        [SerializeField] private GameObject arcParticleBase;
        [SerializeField] private GameObject holdParticleBase;

        [SerializeField] private SpriteRenderer[] laneHighlights;

        private GameObject[] textParticlePool;
        private GameObject[] tapParticlePool;
        private GameObject[] arcParticlePool;

        private int currentTextParticleIndex = 0;
        private int currentTapParticleIndex = 0;
        private int currentArcParticleIndex = 0;
        private Dictionary<int, int> arcGroupToPoolIndex = new Dictionary<int, int>();
        //TODO: SETUP HOLD PARTICLE
        private void SetupPoolArray(ref GameObject[] array, int size, GameObject baseObject)
        {
            array = new GameObject[size];
            for (int i = 0; i < size; i++)
            {
                array[i] = Instantiate(baseObject, transform);
            }
        }

        private bool IncrementOrCycle(ref int toIncrement, int limit)
        {
            toIncrement++;
            if (toIncrement == limit) 
            {
                toIncrement = 0;
                return true;
            }
            return false;
        }

        private void Awake()
        {
            Instance = this;

            SetupPoolArray(ref textParticlePool, textParticlePoolSize, textParticleBase);
            SetupPoolArray(ref tapParticlePool, tapParticlePoolSize, tapParticleBase);
            SetupPoolArray(ref arcParticlePool, arcParticlePoolSize, arcParticleBase);

        }

        //WILL REMOVE LATER
        public void PlayParticleAt(float2 position, JudgeType type)
        {
            PlayTapParticleAt(position, type, JudgeDetail.None);
        }

        public void PlayTapParticleAt(float2 position, JudgeType judgeType, JudgeDetail judgeDetail)
        {
            //Tap effect
            GameObject tap = tapParticlePool[currentTapParticleIndex];
            tap.GetComponent<Transform>().position = new float3(position, 0);
            tap.GetComponent<ParticleSystem>().Play();

            IncrementOrCycle(ref currentTapParticleIndex, tapParticlePoolSize - 1);

            //Lost - Far - Pure
            GameObject text = textParticlePool[currentTextParticleIndex];
            text.GetComponent<Transform>().position = new float3(position, 0);
            text.GetComponent<Renderer>().material = textJudgeMaterials[(int)judgeType];
            text.GetComponent<ParticleSystem>().Play();

            IncrementOrCycle(ref currentTextParticleIndex, textParticlePoolSize - 1);

            //Early - Late
            if (judgeDetail == JudgeDetail.Early) 
            {
                // centreJudgeRenderer.material = EarlyJudgeMaterial;
                // centreJudgeParticleSystem.Play();
            }
            if (judgeDetail == JudgeDetail.Late) 
            {
                // centreJudgeRenderer.material = LateJudgeMaterial;
                // centreJudgeParticleSystem.Play();
            }
        }
    }
}