using ArcCore.Gameplay.Behaviours;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using ArcCore.Gameplay.Data;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(JudgementSystemGroup)), UpdateAfter(typeof(TappableJudgeSystem))]
    public class HoldHighlightSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!PlayManager.IsUpdatingAndActive) return;

            var tracksHeld = PlayManager.InputHandler.tracksHeld;
            int currentTime = PlayManager.ReceptorTime;

            RenderMesh highlightRenderMesh = PlayManager.HighlightHold;
            RenderMesh grayoutRenderMesh = PlayManager.GrayoutHold;

            var commandBuffer = PlayManager.CommandBuffer;

            Entities.WithAll<Translation, ChartIncrTime>().WithNone<PastJudgeRange, HoldLocked>().ForEach( 
                (Entity en, in ChartLane lane) =>
                {
                    if (tracksHeld[lane.lane] > 0)
                        commandBuffer.SetSharedComponent(en, highlightRenderMesh);
                    else
                        commandBuffer.SetSharedComponent(en, grayoutRenderMesh);
                }
            ).WithoutBurst().Run();
            
            Entities.WithAll<Translation, HoldLocked>().WithNone<PastJudgeRange>().ForEach( 
                (Entity en, in ChartTime time) =>
                {
                    if (time.value <= currentTime - Constants.FarWindow)
                    {
                        commandBuffer.SetSharedComponent(en, grayoutRenderMesh);
                    }
                }
            ).WithoutBurst().Run();
        }
    }
}