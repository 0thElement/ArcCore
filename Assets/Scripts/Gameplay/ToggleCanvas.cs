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
        public InputField JumpToTimingInput;
        public void TogglePauseCanvas()
        {
            if (!PauseCanvas.activeSelf)
            {
                PauseCanvas.SetActive(true);
                PlayManager.Pause();
            }
            else
            {
                PauseCanvas.SetActive(false);
                PlayManager.Play();
            }
        }

        public void Restart()
        {
            PlayManager.JumpTo(0);
        }

        public void Jump()
        {
            PlayManager.JumpTo(int.Parse(JumpToTimingInput.text));
        }
    }
}
