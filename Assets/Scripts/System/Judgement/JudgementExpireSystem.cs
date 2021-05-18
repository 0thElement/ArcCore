using ArcCore;
using ArcCore.Behaviours;
using ArcCore.Components;
using ArcCore.Components.Tags;
using Unity.Collections;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class JudgementExpireSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (!GameState.isChartMode) return;

        int lostCount = ScoreManager.Instance.lostCount,
            currentCombo = ScoreManager.Instance.currentCombo;

        float currentTime = Conductor.Instance.receptorTime / 1000f;

        var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

        //- TAPS -//
        Entities.WithAll<WithinJudgeRange>().WithNone<ChartIncrTime, EntityReference>().ForEach(
            (Entity en, in ChartTime chartTime) => {
                if(currentTime - Constants.FarWindow > chartTime.value)
                {
                    commandBuffer.DestroyEntity(en);
                    lostCount++;
                    currentCombo = 0;
                }
            }
        ).Run();

        //- ARCTAPS -//
        Entities.WithAll<WithinJudgeRange>().WithNone<ChartIncrTime>().ForEach(
            (Entity en, in ChartTime chartTime, in EntityReference enRef) => {
                if (currentTime - Constants.FarWindow > chartTime.value)
                {
                    commandBuffer.DestroyEntity(en);
                    commandBuffer.DestroyEntity(enRef.value);
                    lostCount++;
                    currentCombo = 0;
                }
            }
        ).Run();

        //- HOLDS -//
        Entities.WithAll<WithinJudgeRange, ChartLane, ChartTime>().ForEach(
            (Entity en, ref ChartIncrTime chartIncrTime) => {
                if (currentTime - Constants.FarWindow > chartIncrTime.time)
                {
                    if (chartIncrTime.UpdateJudgePointCache((int)currentTime, out int count))
                    {
                        commandBuffer.RemoveComponent<WithinJudgeRange>(en);
                        commandBuffer.AddComponent<PastJudgeRange>(en); 
                        //this ideology might be problematic! (doubling chunk count again)
                    }
                    lostCount += count;
                    currentCombo = 0;
                }
            }
        ).Run();

        //- ARCS -//
        //...

        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();

        ScoreManager.Instance.lostCount = lostCount;
        ScoreManager.Instance.currentCombo = currentCombo;
    }
}