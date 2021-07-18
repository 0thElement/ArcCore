using ArcCore.Gameplay.Data;
using ArcCore.Gameplay.Behaviours;
using ArcCore.Gameplay.Behaviours.EntityCreation;
using ArcCore.Gameplay.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using System.Collections.Generic;

namespace ArcCore.Gameplay.Systems.Judgement
{
    [UpdateInGroup(typeof(JudgementSystemGroup)), UpdateAfter(typeof(ArcCollisionCheckSystem))]
    public class ArcColorSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem entityCommandBufferSystem;
        private RenderMesh initial, highlight, grayout, head, height;
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
            NativeArray<GroupState> arcGroupHeldState = ArcCollisionCheckSystem.arcGroupHeldState;
            List<ArcColorFSM> arcColorFsmArray = ArcCollisionCheckSystem.arcColorFsmArray;

            //Arc segments
            for (int color=0; color < ArcEntityCreator.ColorCount; color++)
            {
                (initial, highlight, grayout, head, height) = ArcEntityCreator.Instance.GetRenderMeshVariants(color);

                float redmix = arcColorFsmArray[color].redmix;
                initial.material.SetFloat(redmixShaderId, redmix);
                highlight.material.SetFloat(redmixShaderId, redmix);
                grayout.material.SetFloat(redmixShaderId, redmix);
                head.material.SetFloat(redmixShaderId, redmix);
                height.material.SetFloat(redmixShaderId, redmix);
                
                Entities.WithSharedComponentFilter<RenderMesh>(initial).ForEach(
                    (Entity en, ref Cutoff cutoff, in ArcGroupID groupID) =>
                    {
                        GroupState state = arcGroupHeldState[groupID.value];

                        if (state == GroupState.Missed)
                        {
                            commandBuffer.SetSharedComponent<RenderMesh>(en, grayout);
                        } 
                        else if (state == GroupState.Lifted)
                        {
                            commandBuffer.SetSharedComponent<RenderMesh>(en, grayout);
                            cutoff.value = true;
                        } 
                        else if (state == GroupState.Held)
                        {
                            commandBuffer.SetSharedComponent<RenderMesh>(en, highlight);
                            cutoff.value = true;
                        } 
                    }
                ).WithoutBurst().Run();
            
                Entities.WithSharedComponentFilter<RenderMesh>(highlight).ForEach(
                    (Entity en, ref Cutoff cutoff, in ArcGroupID groupID) =>
                    {
                        if (arcGroupHeldState[groupID.value] == GroupState.Lifted)
                        {
                            commandBuffer.SetSharedComponent<RenderMesh>(en, grayout);
                        }
                    }
                ).WithoutBurst().Run();

                Entities.WithSharedComponentFilter<RenderMesh>(grayout).ForEach(
                    (Entity en, ref Cutoff cutoff, in ArcGroupID groupID) =>
                    {
                        if (arcGroupHeldState[groupID.value] == GroupState.Held)
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
                (Entity en, ref Cutoff cutoff, in ArcGroupID groupID) =>
                {
                        GroupState state = arcGroupHeldState[groupID.value];

                        if (state == GroupState.Missed)
                        {
                            commandBuffer.SetSharedComponent<RenderMesh>(en, shadowGrayout);
                        } 
                        else if (state == GroupState.Lifted)
                        {
                            commandBuffer.SetSharedComponent<RenderMesh>(en, shadowGrayout);
                            cutoff.value = true;
                        } 
                        else if (state == GroupState.Held)
                        {
                            commandBuffer.SetSharedComponent<RenderMesh>(en, shadowInitial);
                            cutoff.value = true;
                        } 
                }
            ).WithoutBurst().Run();

            Entities.WithSharedComponentFilter<RenderMesh>(shadowGrayout).ForEach(
                (Entity entity, in ArcGroupID groupID) =>
                {
                    if (arcGroupHeldState[groupID.value] == GroupState.Held)
                    {
                        commandBuffer.SetSharedComponent<RenderMesh>(entity, shadowInitial);
                    }
                }
            ).WithoutBurst().Run();
        }
    }
}