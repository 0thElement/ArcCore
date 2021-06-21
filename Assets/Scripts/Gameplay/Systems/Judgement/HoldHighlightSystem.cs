using ArcCore.Gameplay.Behaviours;
using ArcCore.Gameplay.Behaviours.EntityCreation;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using ArcCore.Gameplay.Data;

namespace ArcCore.Gameplay.Systems.Judgement
{
    [UpdateInGroup(typeof(SimulationSystemGroup)), UpdateAfter(typeof(TappableJudgeSystem))]

    public class HoldHighlightSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            NTrackArray<int> tracksHeld = InputManager.Instance.tracksHeld;
            EntityCommandBuffer commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            int currentTime = Conductor.Instance.receptorTime;

            RenderMesh highlightRenderMesh = HoldEntityCreator.Instance.HighlightRenderMesh;
            RenderMesh grayoutRenderMesh = HoldEntityCreator.Instance.GrayoutRenderMesh;

            Entities.WithAll<Translation, ChartIncrTime>().WithNone<PastJudgeRange, HoldLocked>().ForEach( 
                (Entity en, in ChartLane lane) =>
                {
                    if (tracksHeld[lane.lane] > 0)
                        commandBuffer.SetSharedComponent<RenderMesh>(en, highlightRenderMesh);
                    else
                        commandBuffer.SetSharedComponent<RenderMesh>(en, grayoutRenderMesh);
                }
            ).Run();
            
            Entities.WithAll<Translation, HoldLocked>().WithNone<PastJudgeRange>().ForEach( 
                (Entity en, in ChartTime time) =>
                {
                    if (time.value <= currentTime + Constants.FarWindow)
                    {
                        commandBuffer.SetSharedComponent<RenderMesh>(en, grayoutRenderMesh);
                    }
                }
            ).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}