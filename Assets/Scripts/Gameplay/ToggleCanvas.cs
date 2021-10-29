using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Entities;

namespace ArcCore.Gameplay.Behaviours 
{
    public class ToggleCanvas : MonoBehaviour
    {
        public GameObject PauseCanvas;
        public void TogglePauseCanvas()
        {
            if (!PauseCanvas.activeSelf)
            {
                Time.timeScale = 0;
                //Conductor.Instance.isPaused = true;
                //Conductor.Instance.PauseAudio();
                PauseCanvas.SetActive(true);
            }
            else
            {
                Time.timeScale = 1;
                PauseCanvas.SetActive(false);
                //Conductor.Instance.isPaused = false;
                //Conductor.Instance.ResumeAudio();
            }
        }

        public void Restart()
        {
            DestroyAllEntities();          
            PlayManager.LoadChart(Constants.GetDebugChart());
            //PlayManager.PlayMusic();
        }

        void DestroyAllEntities()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            entityManager.DestroyEntity(entityManager.UniversalQuery);
        }
    } 
}
