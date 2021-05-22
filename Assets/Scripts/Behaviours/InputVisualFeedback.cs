using Unity.Mathematics;
using ArcCore.Utility;
using UnityEngine;

namespace ArcCore.Behaviours
{
    public class InputVisualFeedback : MonoBehaviour
    {
        public static InputVisualFeedback Instance { get; private set; }

        [SerializeField] private SpriteRenderer[] laneHighlights;
        [SerializeField] private float highlightDuration = 0.5f; 
        [SerializeField] private float highlightAlpha = 0.5f; 

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            //this is probably faster than using dotween
            for (int i=0; i<4; i++)
            {
                SpriteRenderer lane = laneHighlights[i];
                float alpha = lane.color.a;
                alpha -= Time.deltaTime / highlightDuration * highlightAlpha;
                if (alpha < 0) alpha = 0;
                lane.color = new Color(1, 1, 1, alpha);
            }
        }
        private void HighlightTrack(int track)
        {
            laneHighlights[track].color = new Color(1, 1, 1, highlightAlpha);
        }

        public void PlayLaneEffect(float2 position)
        {
            if (position.y < 2f) HighlightTrack(Conversion.XToTrack(position.x) - 1);
        }
    }
}