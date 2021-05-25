using UnityEngine;

namespace ArcCore.Gameplay.Behaviours
{
    public class InputVisualFeedback : MonoBehaviour
    {
        public static InputVisualFeedback Instance { get; private set; }

        [SerializeField] private SpriteRenderer[] laneHighlights;
        [SerializeField] private float highlightDuration = 0.5f; 
        [SerializeField] private float highlightAlpha = 0.5f; 

        private GameObject[] horizontalLines;
        [SerializeField] private GameObject horizontalLinePrefab;

        private void Awake()
        {
            horizontalLines = new GameObject[10];
            for (int i=0; i<10; i++)
            {
                horizontalLines[i] = Instantiate(horizontalLinePrefab, transform);
            }

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
        public void HighlightLane(int track)
        {
            laneHighlights[track].color = new Color(1, 1, 1, highlightAlpha);
        }
        public void HorizontalLineAt(float height, int fingerid)
        {
            if (height > 5.5f) height = 5.5f; 
            horizontalLines[fingerid].SetActive(true);
            horizontalLines[fingerid].transform.position = new Vector3(0, height, 0);
        }

        public void DisableLines()
        {
            for (int i=0; i<10; i++) horizontalLines[i].SetActive(false);
        }
    }
}