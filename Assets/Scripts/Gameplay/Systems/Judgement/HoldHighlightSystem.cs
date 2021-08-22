using ArcCore.Gameplay.Behaviours;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using ArcCore.Gameplay.Data;
using UnityEngine;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(JudgementSystemGroup)), UpdateAfter(typeof(TappableJudgeSystem))]
    public class HoldHighlightSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!PlayManager.IsUpdatingAndActive) return;
            RenderMesh initialRenderMesh = HoldEntityCreator.Instance.InitialRenderMesh;
            RenderMesh highlightRenderMesh = HoldEntityCreator.Instance.HighlightRenderMesh;
            RenderMesh grayoutRenderMesh = HoldEntityCreator.Instance.GrayoutRenderMesh;
            int currentTime = Conductor.Instance.receptorTime;
            var tracksHeld = PlayManager.InputHandler.tracksHeld;

            RenderMesh highlightRenderMesh = PlayManager.HighlightHold;
            RenderMesh grayoutRenderMesh = PlayManager.GrayoutHold;

            Entities
                .WithSharedComponentFilter<RenderMesh>(grayoutRenderMesh)
                .WithAll<Translation, ChartIncrTime>()
                .WithNone<PastJudgeRange, HoldLocked>()
                .ForEach( 
                    (Entity en, in ChartLane lane) =>
                    {
                        if (tracksHeld[lane.lane] > 0)
                        {
                            commandBuffer.SetSharedComponent<RenderMesh>(en, highlightRenderMesh);
                        }
                    }
                ).WithoutBurst().Run();
                
            Entities
                .WithSharedComponentFilter<RenderMesh>(highlightRenderMesh)
                .WithAll<Translation, ChartIncrTime>()
                .WithNone<PastJudgeRange, HoldLocked>()
                .ForEach( 
                    (Entity en, in ChartLane lane) =>
                    {
                        if (tracksHeld[lane.lane] <= 0)
                        {
                            commandBuffer.SetSharedComponent<RenderMesh>(en, grayoutRenderMesh);
                        }
                    }
                ).WithoutBurst().Run();
            
            Entities
                .WithSharedComponentFilter<RenderMesh>(initialRenderMesh)
                .WithAll<Translation, ChartIncrTime>().WithNone<PastJudgeRange>().ForEach( 
                (Entity en, in ChartLane lane, in ChartTime time) =>
                {
                    if (tracksHeld[lane.lane] <= 0 && time.value <= currentTime - Constants.FarWindow)
                    {
                        commandBuffer.SetSharedComponent(en, grayoutRenderMesh);
                    }
                    else if (tracksHeld[lane.lane])
                    {
                        commandBuffer.SetSharedComponent<RenderMesh>(en, highlightRenderMesh);
                    }
                }
            ).WithoutBurst().Run();
        }
    }
}