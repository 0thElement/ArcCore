using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace ArcCore.UI
{
    public class TransitionSceneOnButtonClick : MonoBehaviour
    {
        //Wtf is all this lol
        /* public static void TransitionScene(string scene)
         {
             TransitionEffect.SetMiddleCoroutine(() => SceneManager.LoadScene(scene, LoadSceneMode.Single));
             TransitionEffect.StartTransition();
         }

         public string sceneName;

         public void Start()
         {
             GetComponent<Button>().onClick.AddListener(() => TransitionScene(sceneName));
             enabled = false;
         }*/

        public string sceneName;
        public void LoadNextScene()
        {
            SceneManager.LoadScene(sceneName);
        }

    }
}