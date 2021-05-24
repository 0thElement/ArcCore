using ArcCore;
using ArcCore.Behaviours;
using ArcCore.Components;
using ArcCore.Components.Tags;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using ArcCore.Structs;
using Unity.Mathematics;
using ArcCore.Utility;
using ArcCore.Math;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class ParticleJudgeSystem : SystemBase
{
    public static ParticleBuffer particleBuffer;
    protected override void OnUpdate()
    {
        if (!GameState.isChartMode) return;

        particleBuffer = new ParticleBuffer(Allocator.TempJob);
    }

    public static void FinalizeFrame()
    {
        particleBuffer.Playback();
        particleBuffer.Dispose();
    }
}