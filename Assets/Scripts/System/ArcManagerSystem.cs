using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using ArcCore.Data;
using ArcCore.MonoBehaviours;
using ArcCore.Tags;

[UpdateBefore(typeof(JudgementSystem))]
public class ArcManagerSystem : SystemBase
{
    public static ArcManagerSystem Instance { get; private set; }
    protected override void OnCreate() => 
        Instance = this;

    public NativeMatrIterator<ArcJudge> arcJudges;
    public NativeArray<ArcCompleteState> arcStates;
    public NativeArray<int> arcFingers;

    public NativeArray<AffArc> rawArcs;

    protected override void OnUpdate()
    {
        if (!arcStates.IsCreated) return;

        for(int i = 0; i < )
    }
}