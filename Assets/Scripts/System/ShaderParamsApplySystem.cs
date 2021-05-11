using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using ArcCore.Components;
using ArcCore.Behaviours;
using Unity.Rendering;
using ArcCore.Structs;

[UpdateAfter(typeof(JudgementSystem))]
public class ShaderParamsApplySystem : SystemBase
{

    protected unsafe override void OnUpdate()
    {
        EntityManager entityManager = EntityManager;
        int currentTime = Conductor.Instance.receptorTime;

        //NativeArray<ArcCompleteState> arcStates = JudgementSystem.Instance.arcStates;
        /*
        //ARCS
        Entities.WithNone<Translation>().ForEach(

            (ref ShaderCutoff cutoff, ref ShaderRedmix redmix, in ColorID color)

                =>

            {
                redmix.Value = arcStates[color.value].redRoll;
                cutoff.Value = arcStates[color.value].alphaRoll; //pls fix 0
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

            (ref ShaderCutoff cutoff, in HoldFunnelPtr holdFunnelPtr) //wtf is this garbage. pls talk to me to find out

                =>

            {
                cutoff.Value = (float)holdFunnelPtr.Value->visualState;
            }

        )
            .WithName("HoldShaders")
            .Schedule();
        */
    }
}
