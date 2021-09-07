using UnityEngine;

namespace ArcCore.UI
{
    [RequireComponent(typeof(Animation))]
    public class TransitionAnimationInterface : MonoBehaviour
    {
        public AnimationClip transitionIn, transitionOut;
        private const string InStr = "_In", OutStr = "_Out";

        private new Animation animation;

        private void Awake()
        {
            animation = GetComponent<Animation>();
            transitionIn.legacy = true;
            transitionOut.legacy = true;
            animation.AddClip(transitionIn, InStr);
            animation.AddClip(transitionOut, OutStr);
        }

        public void TransitionIn()
        {
            animation.Play(InStr);
        }
        public void TransitionOut()
        {
            animation.Play(OutStr);
        }

        public bool IsPlaying 
        {
            get
            {
                return animation.IsPlaying(InStr) || animation.IsPlaying(OutStr);
            }
        }
    }
}