using Unity.Entities;

namespace ArcCore.Components
{
    [GenerateAuthoringComponent]
    public struct ArcdataIndex : IComponentData
    {
        public int value;
        public ArcdataIndex(int value) => this.value = value;
    }
}