using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct TimingGroupSCD : ISharedComponentData
{
    public int value;
}
