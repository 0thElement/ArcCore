using Unity.Entities;
using Unity.Rendering;

namespace ArcCore.Data
{
    [System.Obsolete]
    [MaterialProperty("_ArcState", MaterialPropertyFormat.Float)]
    [GenerateAuthoringComponent]
    public struct ArcState : IComponentData
    {
        public float Value;

        public const float Blank = 0;
        public const float Missed = 1;
        public const float Hit = 2;
    }
}
