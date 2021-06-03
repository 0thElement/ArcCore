using Unity.Mathematics;
using System.Collections.Generic;
using UnityEngine;
using ArcCore.Gameplay.Utility;

namespace ArcCore.Gameplay.Behaviours
{
    public class ParticlePool : MonoBehaviour
    {
        public enum JudgeType
        {
            Lost,
            Far,
            Pure,
            MaxPure
        }
        public enum JudgeDetail
        {
            None,
            Early,
            Late
        }

        public static ParticlePool Instance { get; private set; }

        [SerializeField] private int textParticlePoolSize;
        [SerializeField] private int tapParticlePoolSize;
        [SerializeField] private int arcParticlePoolSize;

        [SerializeField] private Material[] textJudgeMaterials;

        [SerializeField] private GameObject textParticleBase;
        [SerializeField] private GameObject tapParticleBase;
        [SerializeField] private GameObject arcParticleBase;
        
        [SerializeField] private ParticleSystem[] laneParticles;
        [SerializeField] private int laneParticlesBurstCount;
        private float[] laneParticleScheduledStopTime = new float[4];

        private GameObject[] textParticlePool;
        private GameObject[] tapParticlePool;
        private GameObject[] arcParticlePool;

        [SerializeField] private Renderer earlylateJudgeRenderer;
        [SerializeField] private ParticleSystem earlylateJudgeParticleSystem;
        [SerializeField] private Material earlyJudgeMaterial;
        [SerializeField] private Material lateJudgeMaterial;

        private int currentTextParticleIndex = 0;
        private int currentTapParticleIndex = 0;
        private int currentArcParticleIndex = 0;
        private Dictionary<int, int> arcGroupToPoolIndex = new Dictionary<int, int>();

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

        private void TapParticleAt(float2 position)
        {
            GameObject tap = tapParticlePool[currentTapParticleIndex];
            tap.GetComponent<Transform>().position = new float3(position, 0);
            tap.GetComponent<ParticleSystem>().Play();

            IncrementOrCycle(ref currentTapParticleIndex, tapParticlePoolSize - 1);
        }

        private void TextParticleAt(float2 position, JudgeType judgeType, JudgeDetail judgeDetail)
        {
            GameObject text = textParticlePool[currentTextParticleIndex];
            text.GetComponent<Transform>().position = new float3(position, 0);
            text.GetComponent<Renderer>().material = textJudgeMaterials[(int)judgeType];
            text.GetComponent<ParticleSystem>().Play();

            IncrementOrCycle(ref currentTextParticleIndex, textParticlePoolSize - 1);

            //Early - Late
            if (judgeDetail == JudgeDetail.Early) 
            {
                earlylateJudgeRenderer.material = earlyJudgeMaterial;
                earlylateJudgeParticleSystem.Clear();
                earlylateJudgeParticleSystem.Play();
            }
            if (judgeDetail == JudgeDetail.Late) 
            {
                earlylateJudgeRenderer.material = lateJudgeMaterial;
                earlylateJudgeParticleSystem.Clear();
                earlylateJudgeParticleSystem.Play();
            }
        }

        public void TapAt(float2 position, JudgeType judgeType, JudgeDetail judgeDetail, float textYOffset)
        {
            if (judgeType != JudgeType.Lost) TapParticleAt(position);
            position.y += textYOffset;
            TextParticleAt(position, judgeType, judgeDetail);
        }

        public void HoldAt(int lane, bool isHit)
        {
            //probably will break if holds overlap on one lane
            //fuck whoever does that
            float2 position = new float2(Conversion.TrackToX(lane+1), 1.5f);
            if (isHit)
            {
                laneParticles[lane].Play();
                TextParticleAt(position, JudgeType.MaxPure, JudgeDetail.None);
            }
            else
            {
                DisableLane(lane);
                TextParticleAt(position, JudgeType.Lost, JudgeDetail.None);
            }
        }

        public void DisableLane(int track)
        {
            laneParticles[track].Stop();
            laneParticles[track].Clear();
        }
    }
}