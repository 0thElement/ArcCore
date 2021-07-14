using ArcCore.Gameplay.Behaviours;
using ArcCore.Gameplay.Behaviours.EntityCreation;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using ArcCore.Gameplay.Data;

namespace ArcCore.Gameplay.Systems.Judgement
{
    [UpdateInGroup(typeof(JudgementSystemGroup)), UpdateAfter(typeof(TappableJudgeSystem))]

    public class HoldHighlightSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem entityCommandBufferSystem;
        private RenderMesh highlightRenderMesh;
        private RenderMesh grayoutRenderMesh;
        private EntityQuery renderMeshQuery;
        protected override void OnCreate()
        {
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            highlightRenderMesh = HoldEntityCreator.Instance.HighlightRenderMesh;
            grayoutRenderMesh = HoldEntityCreator.Instance.GrayoutRenderMesh;
            renderMeshQuery = GetEntityQuery(typeof(RenderMesh));
        }
        protected override void OnUpdate()
        {
            NTrackArray<ArcCore.Gameplay.Data.MulticountBool> tracksHeld = InputManager.Instance.tracksHeld;
            int currentTime = Conductor.Instance.receptorTime;

            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();

            renderMeshQuery.SetSharedComponentFilter<RenderMesh>(grayoutRenderMesh);
            Entities
                .WithStoreEntityQueryInField(ref renderMeshQuery)
                .WithAll<Translation, ChartIncrTime>()
                .WithNone<PastJudgeRange, HoldLocked>()
                .ForEach( 
                    (Entity en, in ChartLane lane) =>
                    {
                        if (tracksHeld[lane.lane] > 0)
                            commandBuffer.SetSharedComponent<RenderMesh>(en, highlightRenderMesh);
                    }
                ).WithoutBurst().Run();
                
            renderMeshQuery.SetSharedComponentFilter<RenderMesh>(highlightRenderMesh);
            Entities
                .WithStoreEntityQueryInField(ref renderMeshQuery)
                .WithAll<Translation, ChartIncrTime>()
                .WithNone<PastJudgeRange, HoldLocked>()
                .ForEach( 
                    (Entity en, in ChartLane lane) =>
                    {
                        if (tracksHeld[lane.lane] <= 0)
                            commandBuffer.SetSharedComponent<RenderMesh>(en, grayoutRenderMesh);
                    }
                ).WithoutBurst().Run();
            
            Entities.WithAll<Translation, HoldLocked>().WithNone<PastJudgeRange>().ForEach( 
                (Entity en, in ChartTime time) =>
                {
                    if (time.value <= currentTime - Constants.FarWindow)
                    {
                        commandBuffer.SetSharedComponent<RenderMesh>(en, grayoutRenderMesh);
                    }
                }
            ).WithoutBurst().Run();
        }
    }
}