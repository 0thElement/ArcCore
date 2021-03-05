using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// The component used to store note's timing group
/// Contains two properties, <c>startPosition</c>, and <c>endPosition</c>, both of type <c>float3</c>.
/// </summary>
[GenerateAuthoringComponent]
public struct TimingGroup : IComponentData
{
    public int Value;
}
