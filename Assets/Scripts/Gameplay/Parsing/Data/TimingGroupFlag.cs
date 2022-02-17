using System;

namespace ArcCore.Gameplay.Parsing.Data
{
    [Flags]
    public enum TimingGroupFlag
    {
        None = 0,
        NoInput = 0b1,
        NoShadow = 0b10,
        NoHeightIndicator = 0b100,
        Autoplay = 0b1000,
    }
}
