using UnityEngine;
using ArcCore.Gameplay.Parsing.Data;
using ArcCore.Utilities;

namespace ArcCore.Gameplay.Behaviours
{
    public class SpriteScenecontrolData
    {
        public Sprite startSprite;

        public IndexedList<ControlValueKey<Sprite>> imageKeys;
        public IndexedList<ControlValueKey<int>> sortLayerKeys;

        public SpriteScenecontrolData(Sprite startSprite)
        {
            this.startSprite = startSprite;

            imageKeys = new IndexedList<ControlValueKey<Sprite>>();
            sortLayerKeys = new IndexedList<ControlValueKey<int>>();
        }

        public void Reset()
        {
            imageKeys.Reset();
            sortLayerKeys.Reset();
        }
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteScenecontrolObject : ScenecontrolObject
    {
        public SpriteRenderer spriteRenderer;
        public SpriteScenecontrolData spriteData;

        public override void Reset()
        {
            base.Reset();
            spriteData.Reset();

            spriteRenderer.sprite = spriteData.startSprite;
            spriteRenderer.sortingOrder = 0;
        }

        public override void Update()
        {
            base.Update();
            var time = PlayManager.Conductor.receptorTime;

            spriteRenderer.color = GetColor(time);
            enabled = true; //fix alpha bugs

            //sprite order
            Sprite sprite = null;
            while (spriteData.imageKeys.Unfinished && time < spriteData.imageKeys.Current.timing)
            {
                sprite = spriteData.imageKeys.Current.value;
            }
            if (sprite != null) 
                spriteRenderer.sprite = sprite;

            //set sort order
            int? sortLayer = null;
            while(spriteData.sortLayerKeys.Unfinished && time < spriteData.sortLayerKeys.Current.timing)
            {
                sortLayer = spriteData.sortLayerKeys.Current.value;
            }
            if (sortLayer.HasValue)
                spriteRenderer.sortingOrder = sortLayer.Value;
        }
    }
}