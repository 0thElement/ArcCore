using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public static class Extensions
{
    public static float DistanceSquared(this float2 p, float2 q)
    {
        float dx = p.x - q.x;
        float dy = p.y - q.y;
        return dx * dx + dy * dy;
    }
}
