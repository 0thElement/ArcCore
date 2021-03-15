using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using ArcCore.Data;
using ArcCore.MonoBehaviours;
using ArcCore.Tags;
using Unity.Rendering;

[UpdateAfter(typeof(JudgementSystem))]
public class ShaderParamsApplySystem : SystemBase
{

    protected unsafe override void OnUpdate()
    {
        EntityManager entityManager = EntityManager;

        //ARCS
        Entities.WithNone<ChartTime>().ForEach(

            (ref ShaderCutoff cutoff, ref ShaderRedmix redmix, in ArcFunnelPtr arcFunnelPtr)

                =>

            {
                ArcFunnel* arcFunnelPtrD = arcFunnelPtr.Value;

                if (arcFunnelPtrD->isRed)
                {
                    redmix.Value = math.min(redmix.Value + 0.08f, 1);
                }
                else
                {
                    redmix.Value = 0;
                }

                cutoff.Value = (float)arcFunnelPtrD->visualState;
            }

        )
            .WithName("ArcShaders")
            .Schedule();

        Dependency.Complete();

        //HOLDS
        Entities.WithNone<Translation>().ForEach(

            (ref ShaderCutoff cutoff, in HoldFunnelPtr holdFunnelPtr)

                =>

            {
                cutoff.Value = (float)holdFunnelPtr.Value->visualState;
            }

        )
            .WithName("HoldShaders")
            .Schedule();

        Dependency.Complete();

        //ARCTAPS
        Entities.WithoutBurst().WithStructuralChanges().ForEach(

            (Entity entity, in ArctapFunnelPtr arctapFunnelPtr) 

                =>

            {
                if(!arctapFunnelPtr.Value->isExistant) entityManager.DestroyEntity(entity);
            }

        )
            .Run();
    }
}
