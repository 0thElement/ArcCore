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
    public EntityManager globalEntityManager;

    protected override void OnCreate() 
    { 
        var defaultWorld = World.DefaultGameObjectInjectionWorld;
        globalEntityManager = defaultWorld.EntityManager;
    }

    protected override void OnUpdate()
    {
        EntityManager entityManager = globalEntityManager;

        //ARCS
        Entities.WithNone<ChartTime, Translation>().WithAll<ColorID>().ForEach(

            (ref ShaderCutoff cutoff, ref ShaderRedmix redmix, in EntityReference eref)

                =>

            {
                Entity funnel = eref.Value;
                HitState hit = entityManager.GetComponentData<HitState>(funnel);
                ArcIsRed red = entityManager.GetComponentData<ArcIsRed>(funnel);
                if(red.Value || hit.Value != 0)
                {
                    if (red.Value)
                    {
                        redmix.Value = math.min(redmix.Value + 0.08f, 1);
                    }
                    else
                    {
                        redmix.Value = 0;
                    }

                    cutoff.Value = hit.Value;
                }
            }

        )
            .WithName("ArcShaders")
            .Schedule();

        //HOLDS
        Entities.WithNone<Translation>().ForEach(

            (ref ShaderCutoff cutoff, in HitState hit)

                =>

            {
                if (hit.Value != 0)
                {
                    cutoff.Value = hit.Value;
                }
            }

        )
            .WithName("HoldShaders")
            .Schedule();

        //COMPLETE ALL
        Dependency.Complete();
    }
}
