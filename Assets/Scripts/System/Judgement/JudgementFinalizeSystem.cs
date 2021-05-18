using ArcCore;
using ArcCore.Behaviours;
using ArcCore.Components;
using ArcCore.Components.Tags;
using Unity.Collections;
using Unity.Entities;

[UpdateAfter(typeof(JudgementExpireSystem))]
public class JudgementFinalizeSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (!GameState.isChartMode) return;

        ScoreManager.Instance.UpdateScore();
    }
}