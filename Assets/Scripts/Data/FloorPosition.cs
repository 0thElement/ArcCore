using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct FloorPosition : IComponentData
{
    public float Value;
}
