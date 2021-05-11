﻿using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using ArcCore.Components;
using ArcCore.Behaviours;
using Unity.Rendering;

public class MovingNotesSystem : SystemBase
{
    protected override void OnUpdate()
    {
        NativeArray<float> currentFloorPosition = Conductor.Instance.currentFloorPosition;

        //All note except arcs
        Entities.ForEach((ref Translation translation, in FloorPosition floorPosition, in TimingGroup group) => {
            translation.Value.z = floorPosition.value - currentFloorPosition[group.value]; 
        }).Schedule();

        //Arc segments
        Entities.WithNone<Translation>().
            ForEach((ref LocalToWorld lcwMatrix, in FloorPosition floorPosition, in TimingGroup group) =>
            {

                lcwMatrix.Value.c3.z = floorPosition.value - currentFloorPosition[group.value];

            }).Schedule();
    }
}
