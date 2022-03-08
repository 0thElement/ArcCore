using ArcCore.Gameplay.Data;
using ArcCore.Gameplay.Components;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace ArcCore.Gameplay.Systems
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
            if (!PlayManager.IsUpdatingAndActive) return;

            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();

            int currentTime = PlayManager.ReceptorTime;
            var arcGroupHeldState = PlayManager.ArcGroupHeldState;
            var ArcColorStateArray = PlayManager.ArcColorState;

            //Arc segments
            for (int color=0; color <= PlayManager.MaxArcColor; color++)
            {
                (initial, highlight, grayout, head, height) = Skin.Instance.GetArcRenderMeshVariants(color);

                float redmix = ArcColorStateArray[color].Redmix(currentTime);
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
            shadowInitial = Skin.Instance.arcShadowRenderMesh;
            shadowGrayout = Skin.Instance.arcShadowGrayoutRenderMesh;

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