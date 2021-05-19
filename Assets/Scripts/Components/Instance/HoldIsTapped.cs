using ArcCore.Utility;
using Unity.Entities;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent, System.Obsolete("bad idea, loser", true)]
    public struct HoldIsTapped : IComponentData
    {
        public int value;

        public const int False = 0;
        public const int True = -1;
        public const int Reset = 8;

        public HoldIsTapped(int t) 
            => value = t;

        public HoldIsTapped Updated() 
            => new HoldIsTapped { value = value - utils.b2i(value > 0) };

        public bool State => value != 0;
    }
}
