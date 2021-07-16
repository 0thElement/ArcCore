using ArcCore.Gameplay.Data;
using ArcCore.Gameplay.Behaviours;
using ArcCore.Gameplay.Behaviours.EntityCreation;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace ArcCore.Gameplay.Systems.Judgement
{
    [UpdateInGroup(typeof(JudgementSystemGroup)), UpdateAfter(typeof(ArcCollisionCheckSystem))]
    public class ArcColorSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem entityCommandBufferSystem;
        private RenderMesh initial, highlight, grayout;
        private RenderMesh shadowInitial, shadowGrayout;
        private int redmixShaderId;

        protected override void OnCreate()
        {
            redmixShaderId = Shader.PropertyToID("_RedMix");
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();

            int currentTime = Conductor.Instance.receptorTime;
            NativeArray<int> arcGroupHeldState = ArcCollisionCheckSystem.arcGroupHeldState;
            NativeArray<ArcColorTouchData> arcColorTouchDataArray = ArcCollisionCheckSystem.arcColorTouchDataArray;

            //Arc segments
            for (int color=0; color < ArcEntityCreator.ColorCount; color++)
            {
                (initial, highlight, grayout) = ArcEntityCreator.Instance.GetRenderMeshVariants(color);

                float redmix = (currentTime - arcColorTouchDataArray[color].endRedArcSchedule) / Constants.ArcRedArcWindow;
                redmix = Mathf.Clamp(redmix, 0, 0.5f);
                redmix = Mathf.Sin(currentTime / 1000); 

                initial.material.SetFloat(redmixShaderId, redmix);
                highlight.material.SetFloat(redmixShaderId, redmix);
                grayout.material.SetFloat(redmixShaderId, redmix);
                
                Entities.WithSharedComponentFilter<RenderMesh>(initial).ForEach(
                    (Entity en, ref Cutoff cutoff, in ArcGroupID groupID) =>
                    {
                        if (arcGroupHeldState[groupID.value] < 0)
                        {
                            commandBuffer.SetSharedComponent<RenderMesh>(en, initial);
                        }
                        if (arcGroupHeldState[groupID.value] > 0)
                        {
                            commandBuffer.SetSharedComponent<RenderMesh>(en, highlight);
                            cutoff.value = true;
                        }
                    }
                ).WithoutBurst().Run();
            
                Entities.WithSharedComponentFilter<RenderMesh>(highlight).ForEach(
                    (Entity en, in ArcGroupID groupID) =>
                    {
                        if (arcGroupHeldState[groupID.value] < 0)
                        {
                            commandBuffer.SetSharedComponent<RenderMesh>(en, grayout);
                        }
                    }
                ).WithoutBurst().Run();

                Entities.WithSharedComponentFilter<RenderMesh>(grayout).ForEach(
                    (Entity en, ref Cutoff cutoff, in ArcGroupID groupID) =>
                    {
                        if (arcGroupHeldState[groupID.value] > 0)
                        {
                            commandBuffer.SetSharedComponent<RenderMesh>(en, highlight);
                            cutoff.value = true;
                        }
                    }
                ).WithoutBurst().Run();
            }

            //Shadow segments
            shadowInitial = ArcEntityCreator.Instance.ArcShadowRenderMesh;
            shadowGrayout = ArcEntityCreator.Instance.ArcShadowGrayoutRenderMesh;

            Entities.WithSharedComponentFilter<RenderMesh>(shadowInitial).ForEach(
                (Entity entity, ref Cutoff cutoff, in ArcGroupID groupID) =>
                {
                    if (arcGroupHeldState[groupID.value] > 0)
                    {
                        cutoff.value = true;
                    }
                    if (arcGroupHeldState[groupID.value] < 0)
                    {
                        commandBuffer.SetSharedComponent<RenderMesh>(entity, shadowGrayout);
                    }
                }
            ).WithoutBurst().Run();

            Entities.WithSharedComponentFilter<RenderMesh>(shadowGrayout).ForEach(
                (Entity entity, in ArcGroupID groupID) =>
                {
                    if (arcGroupHeldState[groupID.value] > 0)
                    {
                        commandBuffer.SetSharedComponent<RenderMesh>(entity, shadowInitial);
                    }
                }
            ).WithoutBurst().Run();
        }
    }
}