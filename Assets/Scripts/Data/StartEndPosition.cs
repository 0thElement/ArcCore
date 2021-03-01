using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Data
{
    /// <summary>
    /// The component used to store the positions of the start and end of arcs and traces.
    /// Contains two properties, <c>startPosition</c>, and <c>endPosition</c>, both of type <c>float3</c>.
    /// </summary>
    [GenerateAuthoringComponent]
    public struct StartEndPosition : IComponentData
    {
        /// <summary>
        /// The position of the start of the component.
        /// </summary>
        public float3 startPosition;
        /// <summary>
        /// The position of the end of the component.
        /// </summary>
        public float3 endPosition;
    }
}