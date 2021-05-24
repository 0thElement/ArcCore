using ArcCore;
using ArcCore.Behaviours;
using ArcCore.Components;
using ArcCore.Components.Tags;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

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