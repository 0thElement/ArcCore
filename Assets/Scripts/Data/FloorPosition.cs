using Unity.Entities;

/// <summary>
/// The component used to store the "floor position" of notes.
/// It holds a single double.
/// </summary>
[GenerateAuthoringComponent]
public struct FloorPosition : IComponentData
{
    public float Value;
}
