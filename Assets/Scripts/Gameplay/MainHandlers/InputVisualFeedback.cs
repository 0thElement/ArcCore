using UnityEngine;
using Unity.Mathematics;
using ArcCore.Utilities;
using ArcCore.Gameplay.Data;
using ArcCore.Storage;

namespace ArcCore.Gameplay.Behaviours
{
    public class InputVisualFeedback : MonoBehaviour
    {
        /// <summary>
        /// The lane highlight renderers.
        /// </summary>
        [SerializeField] private SpriteRenderer[] laneHighlights;
        /// <summary>
        /// The duration which a highlight remains after a track is released.
        /// </summary>
        [SerializeField] private float highlightDuration = 0.25f; 
        /// <summary>
        /// The alpha of a highlight at "full" alpha.
        /// </summary>
        [SerializeField] private float highlightAlpha = 0.15f; 

        /// <summary>
        /// An array of <see cref="InputHandler.MaxTouches"/> horizontal line effects for use.
        /// </summary>
        private GameObject[] horizontalLines;
        /// <summary>
        /// The prefab for the horizontal lines stored in <see cref="horizontalLines"/>
        /// </summary>
        [SerializeField] private GameObject horizontalLinePrefab;

        [Header("Debug")]
        [SerializeField] private GameObject debugArcVisualPrefab;
        private SpriteRenderer[] debugArcVisual;

        private void Awake()
        {
            horizontalLines = new GameObject[InputHandler.MaxTouches];
            for (int i=0; i<10; i++)
            {
                horizontalLines[i] = Instantiate(horizontalLinePrefab, transform);
            }
#if DEBUG
            debugArcVisual = new SpriteRenderer[InputHandler.MaxTouches];
            for (int i=0; i<10; i++)
            {
                debugArcVisual[i] = Instantiate(debugArcVisualPrefab, transform).GetComponent<SpriteRenderer>();
            }
#endif
        }

        private void Update()
        {
            if (!PlayManager.IsUpdating) return;

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

        /// <summary>
        /// Highlight the given track.
        /// </summary>
        public void HighlightLane(int track)
        {
            laneHighlights[track].color = new Color(1, 1, 1, highlightAlpha);
            laneHighlights[track].enabled = true; 
            //unity you can't just fucking go on and disable alpha = 0 sprites without telling anybody anything ESPECIALLY SINCE YOU DON"T EVEN FUCKING DO IT IN THE EDITOR WHY DO YOU DO THIS AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAa
        }
        /// <summary>
        /// Create a horizontal line at a given height and with a given index.
        /// </summary>
        public void HorizontalLineAt(float height, int fingerid)
        {
            height = math.min(height, Constants.InputMaxY);
            horizontalLines[fingerid].SetActive(true);
            horizontalLines[fingerid].transform.position = new Vector3(0, height, 0);
        }

        /// <summary>
        /// Disable all lines.
        /// </summary>
        public void Reset()
        {
            for (int i=0; i<10; i++) horizontalLines[i].SetActive(false);
#if DEBUG
            for (int i=0; i<10; i++) debugArcVisual[i].gameObject.SetActive(false);
#endif
        }

#if DEBUG
        public void DebugArcState(float2 position, ArcColorFSM fsm)
        {
            if (fsm == null) return;
            int color = fsm.Color;
            debugArcVisual[color].gameObject.SetActive(true);
            debugArcVisual[color].transform.position = new Vector3(position.x, position.y, 0);

            Color c = new Color(0,0,0,0);
            if (fsm != null)
                c = Settings.ArcColors[color];
            debugArcVisual[color].color = c;
        }
#endif
    }
}