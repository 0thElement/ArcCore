using System.Collections.Generic;
using UnityEngine;

namespace ArcCore.Utilities
{
    public static class SpriteUtils
    {
        public static Sprite CreateCentered(Texture2D tex) 
            => Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one / 2);
    }
}