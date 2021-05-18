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
            TapEffectType
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

        public void CreateParticle(float2 position, ParticleType type)
        {
            GameObject newObj;

            switch (type) 
            {
                case ParticleType.PureJudgeType:
                    newObj = Instantiate(pureJudgeBase);
                    break;

                case ParticleType.FarJudgeType:
                    newObj = Instantiate(farJudgeBase);
                    break;

                case ParticleType.LostJudgeType:
                    newObj = Instantiate(lostJudgeBase);
                    break;

                default:
                    //default in case of error:
                    newObj = Instantiate(lostJudgeBase);
                    break;
            }

            newObj.transform.position = new Vector3(position.x, position.y);
        }
    }
}