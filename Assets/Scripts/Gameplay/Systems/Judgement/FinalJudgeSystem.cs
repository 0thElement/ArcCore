using ArcCore;
using ArcCore.Gameplay.Behaviours;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace ArcCore.Gameplay.Systems.Judgement
{
    [UpdateInGroup(typeof(SimulationSystemGroup)), UpdateAfter(typeof(UnlockedHoldJudgeSystem))]
    public class FinalJudgeSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!GameState.isChartMode) return;

            ScoreManager.Instance.UpdateScore();

            if (ParticleJudgeSystem.particleBuffer.IsCreated)
            {
                ParticleJudgeSystem.FinalizeFrame();
            }
        }
    }
}