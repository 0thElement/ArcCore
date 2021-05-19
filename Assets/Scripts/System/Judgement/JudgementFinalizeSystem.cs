using ArcCore;
using ArcCore.Behaviours;
using ArcCore.Components;
using ArcCore.Components.Tags;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup)), UpdateAfter(typeof(JudgementExpireSystem))]
public class JudgementFinalizeSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (!GameState.isChartMode) return;

        ScoreManager.Instance.UpdateScore();

        if (JudgementExpireSystem.particleBuffer.IsCreated)
        {
            JudgementExpireSystem.particleBuffer.Playback();
            JudgementExpireSystem.particleBuffer.Dispose();
        }
    }
}