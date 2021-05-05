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
        int currentTime = Conductor.Instance.receptorTime;

        NativeArray<ArcCompleteState> arcStates = JudgementSystem.Instance.arcStates;

        //ARCS
        Entities.WithNone<Translation>().ForEach(

            (ref ShaderCutoff cutoff, ref ShaderRedmix redmix, in ColorID color)

                =>

            {
                redmix.Value = arcStates[color.Value].redRoll;
                cutoff.Value = arcStates[color.Value].alphaRoll;
            }

        )
            .WithName("ArcShaders")
            .Schedule();

        //TRACES
        Entities.WithNone<Translation>().ForEach(

            (ref ShaderCutoff cutoff, in ChartTime time)

                =>

            {
                cutoff.Value = time.value < currentTime ? 1f : 0f;
            }

        )
            .WithName("TraceShaders")
            .Schedule();

        //HOLDS
        Entities.ForEach(

            (ref ShaderCutoff cutoff, in HoldFunnelPtr holdFunnelPtr)

                =>

            {
                cutoff.Value = (float)holdFunnelPtr.Value->visualState;
            }

        )
            .WithName("HoldShaders")
            .Schedule();
    }
}
