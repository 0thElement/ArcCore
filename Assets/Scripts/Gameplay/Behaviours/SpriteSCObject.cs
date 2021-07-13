using UnityEngine;
using ArcCore.Parsing.Data;
using ArcCore.Utilities;

namespace ArcCore.Gameplay.Behaviours
{
    public class SpriteSCObject : BaseSCObject
    {
        [HideInInspector]
        public SpriteRenderer spriteRenderer;

        public Sprite startSprite;

        public IndexedArray<ControlImageKey> imageKeys;
        public IndexedArray<ControlIntKey> sortLayerKeys;

        public new void Reset()
        {
            base.Reset();

            spriteRenderer.sprite = startSprite;
            spriteRenderer.sortingOrder = 0;
        }

        public void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public new void Update()
        {
            base.Update();
            var time = Conductor.Instance.receptorTime;

            spriteRenderer.color = GetColor(time);
            enabled = true; //fix alpha bugs

            //sprite order
            Sprite sprite = null;
            while (imageKeys.Unfinished && time < imageKeys.Current.timing)
            {
                sprite = imageKeys.Current.sprite;
            }
            if (sprite != null) 
                spriteRenderer.sprite = sprite;

            //set sort order
            int? sortLayer = null;
            while(sortLayerKeys.Unfinished && time < sortLayerKeys.Current.timing)
            {
                sortLayer = sortLayerKeys.Current.value;
            }
            if (sortLayer.HasValue)
                spriteRenderer.sortingOrder = sortLayer.Value;
        }
    }
}