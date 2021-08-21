using Unity.Mathematics;
using System.Collections.Generic;
using UnityEngine;
using ArcCore.Gameplay.Utility;

namespace ArcCore.Gameplay.Behaviours
{
    public class ParticlePool : MonoBehaviour
    {
        /// <summary>
        /// The judge type of a given judge particle.
        /// <para>
        /// NOTE: <see cref="JudgeType.Pure"/> and <see cref="JudgeType.MaxPure"/> are distinct.
        /// </para>
        /// </summary>
        public enum JudgeType
        {
            Lost,
            Far,
            Pure,
            MaxPure
        }
        /// <summary>
        /// The detail attached to a given judge particle.
        /// </summary>
        public enum JudgeDetail
        {
            None,
            Early,
            Late
        }

        public static ParticlePool Instance { get; private set; }

        /// <summary>
        /// The size of the text particle pool.
        /// </summary>
        [SerializeField] private int textParticlePoolSize;
        /// <summary>
        /// The size of the tap particle pool.
        /// </summary>
        [SerializeField] private int tapParticlePoolSize;

        /// <summary>
        /// The materials of all the text judges.
        /// <list type="table">
        /// <listheader>The following order must be maintained.</listheader>
        /// <item><term>Lost</term> 0</item>
        /// <item><term>Far</term> 1</item>
        /// <item><term>Pure</term> 2</item>
        /// <item><term>Maximum Pure</term> 3</item>
        /// </list>
        /// </summary>
        [SerializeField] private Material[] textJudgeMaterials;

        /// <summary>
        /// The base game object for text particles.
        /// </summary>
        [SerializeField] private GameObject textParticleBase;
        /// <summary>
        /// The base game object for tap particles.
        /// </summary>
        [SerializeField] private GameObject tapParticleBase;

        /// <summary>
        /// The lane particle systems.
        /// </summary>
        [SerializeField] private ParticleSystem[] laneParticles;
        [SerializeField] private int laneParticlesBurstCount;
        private float[] laneParticleScheduledStopTime = new float[4];

        /// <summary>
        /// The particle pool for text particles.
        /// </summary>
        private GameObject[] textParticlePool;
        /// <summary>
        /// The particle pool for tap particles.
        /// </summary>
        private GameObject[] tapParticlePool;

        /// <summary>
        /// The renderer used to display early/late judgement detail when necessary.
        /// </summary>
        [SerializeField] private Renderer earlylateJudgeRenderer;
        /// <summary>
        /// The particle system used to display early/late judgement detail when necessary.
        /// </summary>
        [SerializeField] private ParticleSystem earlylateJudgeParticleSystem;
        /// <summary>
        /// The material for early judge text.
        /// </summary>
        [SerializeField] private Material earlyJudgeMaterial;
        /// <summary>
        /// The material for late judge text.
        /// </summary>
        [SerializeField] private Material lateJudgeMaterial;

        /// <summary>
        /// The current free index of the text particle pool.
        /// </summary>
        private int currentTextParticleIndex = 0;
        /// <summary>
        /// The current free index of the tap particle pool.
        /// </summary>
        private int currentTapParticleIndex = 0;

        /// <summary>
        /// Set <paramref name="array"/> to an array of size <paramref name="size"/> filled with copies of <paramref name="baseObject"/>,
        /// positioned at this instance's transform.
        /// </summary>
        private void SetupPoolArray(ref GameObject[] array, int size, GameObject baseObject)
        {
            array = new GameObject[size];
            for (int i = 0; i < size; i++)
            {
                array[i] = Instantiate(baseObject, transform);
                array[i].SetActive(false);
            }
        }

        /// <summary>
        /// Increment <paramref name="toIncrement"/> and wrap around to 0 if it reaches <paramref name="limit"/>.
        /// </summary>
        /// <returns><see langword="true"/> if the value is wrapped, <see langword="false"/> otherwise.</returns>
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
        }

        /// <summary>
        /// Create a tap particle at the given position.
        /// </summary>
        private void TapParticleAt(float2 position)
        {
            GameObject tap = tapParticlePool[currentTapParticleIndex];
            tap.SetActive(true);
            tap.GetComponent<Transform>().position = new float3(position, 0);
            tap.GetComponent<ParticleSystem>().Play();

            IncrementOrCycle(ref currentTapParticleIndex, tapParticlePoolSize - 1);
        }

        /// <summary>
        /// Create a text particle at the given position with the given type.
        /// If needed, create the detail particle.
        /// </summary>
        private void TextParticleAt(float2 position, JudgeType judgeType, JudgeDetail judgeDetail)
        {
            GameObject text = textParticlePool[currentTextParticleIndex];
            text.SetActive(true);
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

        /// <summary>
        /// Create all necessary particles from a tap at a given position, of a given judge type and detail, and with a given
        /// y-offset to account for in spawning the textYOffset.
        /// </summary>
        public void TapAt(float2 position, JudgeType judgeType, JudgeDetail judgeDetail, float textYOffset)
        {
            if (judgeType != JudgeType.Lost) TapParticleAt(position);
            position.y += textYOffset;
            TextParticleAt(position, judgeType, judgeDetail);
        }

        /// <summary>
        /// Create particles for a hold note which is judged at the current time, at the given lane, and is hit or not specified by <paramref name="isHit"/>.
        /// </summary>
        public void HoldAt(int lane, bool isHit)
        {
            float2 position = new float2(Conversion.TrackToX(lane+1), 1.5f);
            if (isHit)
            {
                laneParticles[lane].Play();
                TextParticleAt(position, JudgeType.MaxPure, JudgeDetail.None);
            }
            else
            {
                if (!InputManager.Instance.tracksHeld[lane+1])
                {
                    DisableLane(lane);
                }
                TextParticleAt(position, JudgeType.Lost, JudgeDetail.None);
            }
        }
       

        /// <summary>
        /// Disable lane particles for the given lane.
        /// </summary>
        public void DisableLane(int track)
        {
            laneParticles[track].Stop();
            laneParticles[track].Clear();
        }

        public void ArcAt(float2 position, bool isHit)
        {
            var type = isHit ? JudgeType.MaxPure : JudgeType.Lost;
            position.y += 0.5f;
            TextParticleAt(position, type, JudgeDetail.None);
        }
    }
}