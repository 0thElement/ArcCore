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

        private List<TransitionAnimationInterface> animators;

        public void Awake()
        {
            DontDestroyOnLoad(this);
            Instance = this;

            gameObject.SetActive(false);

            animators = new List<TransitionAnimationInterface>();
            animatorDict = new Dictionary<string, TransitionAnimationInterface>();

            foreach(var entry in entries)
            {
                if(entry.isActiveByDefault)
                {
                    animators.Add(entry.animation);
                }
                animatorDict.Add(entry.name, entry.animation);
            }

            entries = null;
        }

        public static void AddAnimator(string name)
            => Instance.AddAnimatorInstance(name);
        public static void AddAnimators(params string[] names)
        {
            foreach (var name in names)
            {
                AddAnimator(name);
            }
        }
        private void AddAnimatorInstance(string name) 
        {
            if(!animatorDict.ContainsKey(name)) 
                throw new Exception("Unfound animators provided to SetTransitionAnimators.");

            animators.Add(animatorDict[name]);
        }

        public static void RemoveAnimator(string name)
            => Instance.RemoveAnimatorInstance(name);
        public static void RemoveAnimators(params string[] names)
        {
            foreach(var name in names)
            {
                RemoveAnimator(name);
            }
        }
        private void RemoveAnimatorInstance(string name)
        {
            if (!animatorDict.ContainsKey(name))
                throw new Exception("Unfound animators provided to SetTransitionAnimators.");
             
            animators.Remove(animatorDict[name]);
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

            StartCoroutine(CTransition());
        }

        private IEnumerator CTransition()
        {
            foreach (var anim in animators)
            {
                anim.TransitionIn();
            }

            while(animators.Any(v => v.IsPlaying))
            {
                yield return null;
            }

            if (middleCoroutine != null)
            {
                while (middleCoroutine.MoveNext())
                {
                    yield return middleCoroutine.Current;
                }

                middleCoroutine = null;
            }

            foreach (var anim in animators)
            {
                anim.TransitionOut();
            }

            while (animators.Any(v => v.IsPlaying))
            {
                yield return null;
            }

            inTransition = false;
            gameObject.SetActive(false);
        }
    }
}