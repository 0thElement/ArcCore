using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Lean.Touch;
using ArcCore.Gameplay;
using ArcCore.Gameplay.EntityCreation;

namespace ArcCore.Gameplay.Behaviours 
{
    public class ToggleCanvas : MonoBehaviour
    {
        public GameObject PauseCanvas;
        public InputHandler touchHandler;
        public void TogglePauseCanvas()
        {
            if (!PauseCanvas.activeSelf)
            {            
                PlayManager.Conductor.PauseMusic();
                touchHandler.enabled = false;
                PauseCanvas.SetActive(true);
            }
            else
            {
                
                PlayManager.Conductor.ResumeMusic();
                touchHandler.enabled = true;
                PauseCanvas.SetActive(false);
            }
        }

        public void Restart()
        {
            DestroyAllEntities();
            PlayManager.ArcIndicatorHandler.Destroy();
            PlayManager.TraceIndicatorHandler.Destroy();
            touchHandler.enabled = true;
            PauseCanvas.SetActive(false);
            PlayManager.Conductor.GetAudioSource().Stop();

            PlayManager.LoadChart(Constants.GetDebugChart());
            PlayManager.Conductor.PlayMusic();
        }

        void DestroyAllEntities()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            entityManager.DestroyEntity(entityManager.UniversalQuery);
        }
    } 
}
