using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

namespace ArcCore.UI
{
    public class TransitionOnButtonClick : MonoBehaviour
    {
        public static void TransitionScene(string scene)
        {
            TransitionEffect.AutoTransition(() => SceneManager.LoadScene(scene, LoadSceneMode.Single));
        }

        public string sceneName;

        public void Start()
        {
            GetComponent<Button>().onClick.AddListener(() => TransitionScene(sceneName));
            enabled = false;
        }
    }
}