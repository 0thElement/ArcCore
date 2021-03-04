using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// The component used to store the positions of the start and end of arcs and traces.
/// Contains two properties, <c>startPosition</c>, and <c>endPosition</c>, both of type <c>float3</c>.
/// </summary>
[GenerateAuthoringComponent]
public struct ArcStartPosition : IComponentData
{
    public float2 Value;
}
