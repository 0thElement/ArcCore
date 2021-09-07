using UnityEngine;

namespace ArcCore.Parsing.Data
{
    public struct ControlImageKey
    {
        private int _timing;
        public int timing
        {
            get => _timing;
            set => _timing = GameSettings.Instance.GetSpeedModifiedTime(value);
        }

        public Sprite sprite;
    }
}
