using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[GenerateAuthoringComponent]
public struct StartEndPosition : IComponentData 
{
    public float3 startPosition;
    public float3 endPosition;
}
