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
            RenderMesh initialRenderMesh = PlayManager.HoldInitialRenderMesh;
            RenderMesh highlightRenderMesh = PlayManager.HoldHighlightRenderMesh;
            RenderMesh grayoutRenderMesh = PlayManager.HoldGrayoutRenderMesh;
            int currentTime = PlayManager.ReceptorTime;
            var tracksHeld = PlayManager.InputHandler.tracksHeld;

            Entities
                .WithSharedComponentFilter<RenderMesh>(grayoutRenderMesh)
                .WithAll<Translation, ChartIncrTime>()
                .WithNone<PastJudgeRange, HoldLocked>()
                .ForEach( 
                    (Entity en, in ChartLane lane) =>
                    {
                        if (tracksHeld[lane.lane] > 0)
                        {
                            PlayManager.CommandBuffer.SetSharedComponent<RenderMesh>(en, highlightRenderMesh);
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
                            PlayManager.CommandBuffer.SetSharedComponent<RenderMesh>(en, grayoutRenderMesh);
                        }
                    }
                ).WithoutBurst().Run();
            
            Entities
                .WithSharedComponentFilter<RenderMesh>(initialRenderMesh)
                .WithAll<Translation, ChartIncrTime>().WithNone<PastJudgeRange, HoldLocked>().ForEach( 
                (Entity en, in ChartLane lane, in ChartTime time) =>
                {
                    if (tracksHeld[lane.lane] <= 0 && time.value <= currentTime - Constants.FarWindow)
                    {
                        PlayManager.CommandBuffer.SetSharedComponent(en, grayoutRenderMesh);
                    }
                    else if (tracksHeld[lane.lane] > 0)
                    {
                        PlayManager.CommandBuffer.SetSharedComponent<RenderMesh>(en, highlightRenderMesh);
                    }
                }
            ).WithoutBurst().Run();
        }
    }
}