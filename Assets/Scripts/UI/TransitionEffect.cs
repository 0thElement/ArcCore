using ArcCore.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityCoroutineUtils;

namespace ArcCore.UI
{
    [RequireComponent(typeof(Animation))]
    public class TransitionAnimationInterface : MonoBehaviour
    {
        public AnimationClip transitionIn, transitionOut;
        private const string InStr = "_In", OutStr = "_Out";

        private new Animation animation;
        private bool isSetup;

        private void EnsureAnimationSetup()
        {
            if (isSetup) return;

            animation.AddClip(transitionIn, InStr);
            animation.AddClip(transitionOut, OutStr);
            isSetup = true;
        }

        public void TransitionIn()
        {
            EnsureAnimationSetup();
            animation.Play(InStr);
        }
        public void TransitionOut()
        {
            EnsureAnimationSetup();
            animation.Play(OutStr);
        }
        public bool IsPlaying 
        {
            get
            {
                EnsureAnimationSetup();
                return animation.IsPlaying(InStr) || animation.isPlaying(OutStr);
            }
        }
    }

    public class TransitionEffect : MonoBehaviour
    {
        public static TransitionEffect Instance { get; private set; }
        public static bool InTransition => Instance.inTransition;

        private IEnumerator middleCoroutine = null;
        private bool inTransition = false;
        
        [Serializable]
        private struct AnimationEntry
        {
            public string name;
            public TransitionAnimationInterface animation;
            public bool isActiveByDefault;
        }

        [SerializeField]
        private AnimationEntry[] entries;
        private Dictionary<string, TransitionAnimationInterface> animatorDict;

        private readonly List<TransitionAnimationInterface> transitionAnimators;

        public void Awake()
        {
            DontDestroyOnLoad(this);
            Instance = this;

            gameObject.SetActive(false); 
            
            transitionAnimators = new List<TransitionAnimationInterface>();

            animatorDict = new Dictionary<string, Animator>();
            foreach(var entry in entries)
            {
                if(entry.isActiveByDefault)
                {
                    transitionAnimators.Add(entry.animation);
                }
                animatorDict.Add(entry.name, entry.animation);
            }

            entries = null;
        }

        public static void SetTransitionAnimators(string[] transitionAnimatorNames)
            => Instance.SetTransitionAnimatorsInstance(transitionAnimatorNames);
        private void SetTransitionAnimatorsInstance(string[] transitionAnimatorNames) 
        {
            if (inTransition)
                throw new Exception("Cannot set transion animators while currently in transition!");

            transitionAnimators.Clear();

            foreach(var name in transitionAnimatorNames)
            {
                if(!animatorDict.ContainsKey(name)) 
                    throw new Exception("Unfound animators provided to SetTransitionAnimators.");

                transitionAnimators.Add(animatorDict[name]);
            }
        }

        public static void SetMiddleCoroutine(IEnumerator coroutine)
        {
            if (Instance.inTransition)
                throw new Exception("Cannot set middle coroutine while currently in transition!");
            Instance.middleCoroutine = coroutine;
        }
        public static void SetMiddleCoroutine(Action coroutine)
        {
            IEnumerator Coroutine()
            {
                coroutine();
                yield break;
            }
            SetMiddleCoroutine(Coroutine());
        }

        public void OnDestroy()
        {
            Instance = null;
        }

        public static void StartTransition()
            => Instance.StartTransitionInstance();
        public void StartTransitionInstance()
        {
            gameObject.SetActive(true);
            inTransition = true;
            CTransition().Start(this);
        }
        private IEnumerator CTransition()
        {
            foreach (var anim in transitionAnimators)
            {
                anim.TransitionIn();
            }

            while(transitionAnimators.Any(v => v.IsPlaying))
            {
                yield return null;
            }

            yield return middleCoroutine;

            foreach (var anim in transitionAnimators)
            {
                anim.TransitionOut();
            }

            while (transitionAnimators.Any(v => v.IsPlaying))
            {
                yield return null;
            }

            inTransition = false;
            gameObject.SetActive(false);
        }
    }
}