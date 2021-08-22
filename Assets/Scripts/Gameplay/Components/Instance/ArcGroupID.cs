using Unity.Entities;

namespace ArcCore.Gameplay.Components
{
    [GenerateAuthoringComponent]
    public struct ArcGroupID : IComponentData
    {
        /// <summary>
        /// Group id value of the arc instance.
        /// A set of arcs are considered to be in the same group (have the same group id) if they 'connect' into a single continous curve.
        /// </summary>
        public int value;

        public ArcGroupID(int value)
            => this.value = value;
    }
}
