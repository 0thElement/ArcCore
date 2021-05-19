using Unity.Mathematics;
using UnityEngine;

namespace ArcCore.Behaviours
{
    public class ParticleCreator : MonoBehaviour
    {
        public enum ParticleType
        {
            LostJudgeType,
            PureJudgeType,
            FarJudgeType,
            TapEffectType,
            Void
        }

        public static ParticleCreator Instance { get; private set; }

        public GameObject lostJudgeBase,
            pureJudgeBase,
            farJudgeBase,
            tapEffectBase;

        //later use
        public Sprite tapEffectSprite;

        private void Awake()
        {
            Instance = this;
        }

        public void PlayParticleAt(float2 position, ParticleType type)
        {
            
        }
    }
}