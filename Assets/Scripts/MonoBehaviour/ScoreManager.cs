using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ArcCore.MonoBehaviours
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        public float MAX_VALUE = 10_000_000f;

        [HideInInspector] public int maxCombo;

        [HideInInspector] public int maxPureCount;
        [HideInInspector] public int latePureCount;
        [HideInInspector] public int earlyPureCount;
        [HideInInspector] public int lateFarCount;
        [HideInInspector] public int earlyFarCount;
        [HideInInspector] public int lostCount;

        [HideInInspector] public float currentScore;

        public Text textUI;

        void Awake()
        {
            Instance = this;
        }

        //call later
        public void UpdateScore()
        {
            currentScore =
                (maxPureCount + latePureCount + earlyPureCount) * MAX_VALUE / maxCombo +
                (lateFarCount + earlyFarCount) * MAX_VALUE / maxCombo / 2 +
                maxPureCount;
            textUI.text = $"{(int)currentScore:D8}";
        }
    }
}