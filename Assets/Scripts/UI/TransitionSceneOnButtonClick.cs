using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace ArcCore.UI
{
    public class TransitionSceneOnButtonClick : MonoBehaviour
    {
        public static void TransitionScene(string scene)
        {
            TransitionEffect.SetMiddleCoroutine(() => SceneManager.LoadScene(scene, LoadSceneMode.Single));
            TransitionEffect.StartTransition();
        }

        public string sceneName;

        public void Start()
        {
            GetComponent<Button>().onClick.AddListener(() => TransitionScene(sceneName));
            enabled = false;
        }
    }
}