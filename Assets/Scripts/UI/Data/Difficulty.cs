using UnityEngine;

namespace ArcCore.UI.Data
{
    public class Difficulty
    {
        public Color Color { get; set; }
        public string Name { get; set; }
        public string LevelName { get; set; }
        public bool IsPlus { get; set; }
        public int Precedence { get; set; }
    }
}