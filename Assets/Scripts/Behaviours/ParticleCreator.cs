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
            ScreenSprite newObj;

            switch (type) 
            {
                case ParticleType.PureJudgeType:
                    newObj = Instantiate(pureJudgeBase).GetComponent<ScreenSprite>();
                    break;

                case ParticleType.FarJudgeType:
                    newObj = Instantiate(farJudgeBase).GetComponent<ScreenSprite>();
                    break;

                case ParticleType.LostJudgeType:
                    newObj = Instantiate(lostJudgeBase).GetComponent<ScreenSprite>();
                    break;

                default:
                    //default in case of error:
                    newObj = Instantiate(lostJudgeBase).GetComponent<ScreenSprite>();
                    break;
            }

            var proj = Camera.main.WorldToScreenPoint(new Vector3(position.x, position.y, 0));
            newObj.screenPos = new float2(proj.x, proj.y);
        }
    }
}