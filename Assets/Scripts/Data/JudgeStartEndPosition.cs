﻿using Unity.Entities;
using Unity.Mathematics;

namespace ArcCore.Data
{
    [GenerateAuthoringComponent]
    public struct JudgeStartEndPosition : IComponentData
    {
        public float2 startPosition;
        public float2 endPosition;
    }
}