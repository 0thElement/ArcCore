using System.Collections;
using UnityEngine;

namespace ArcCore.MonoBehaviours
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }
        public int maxCombo;

        void Awake()
        {
            Instance = this;
        }
    }
}