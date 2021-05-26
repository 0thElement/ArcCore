using ArcCore;
using ArcCore.Gameplay.Behaviours;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using ArcCore.Gameplay.Data;
using Unity.Mathematics;
using ArcCore.Gameplay.Utility;
using ArcCore.Math;

namespace ArcCore.Gameplay.Systems.Judgement
{
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
}