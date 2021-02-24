using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct FloorPosition : IComponentData
{
    public float value;
}
