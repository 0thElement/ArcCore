using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Mathematics;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using ArcCore.Gameplay.Behaviours;
using UnityEngine;
using Unity.Rendering;
using ArcCore.Gameplay.Systems;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(CustomTransformSystemGroup))]
    public class MovingNotesSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!PlayManager.IsUpdatingAndActive) return;

            NativeArray<float> currentFloorPosition = PlayManager.Conductor.currentFloorPosition;
            int currentTime = PlayManager.Conductor.receptorTime;

            //Beatlines
            Entities.WithNone<TimingGroup>().ForEach((ref Translation translation, in FloorPosition floorPosition) =>
            {
                translation.Value.z = floorPosition.value - currentFloorPosition[0];
            }).ScheduleParallel();

            //All note except arcs and holds
            Entities.WithNone<ChartIncrTime>().ForEach((ref Translation translation, in FloorPosition floorPosition, in TimingGroup group) =>
            {
                translation.Value.z = floorPosition.value - currentFloorPosition[group.value];
            }).ScheduleParallel();

            Entities.WithAll<HoldLocked>().ForEach((ref Translation translation, in FloorPosition floorPosition, in TimingGroup group) =>
            {
                translation.Value.z = floorPosition.value - currentFloorPosition[group.value];
            }).ScheduleParallel();

            //Hold Cutoff
            Entities.WithAll<ChartIncrTime>().WithNone<HoldLocked>().
            ForEach((ref Translation translation, ref NonUniformScale scale, in BaseLength length, in FloorPosition floorposition, in TimingGroup group) =>
            {
                translation.Value.z = 0;
                float newlength = length.value - floorposition.value + currentFloorPosition[group.value];
                scale.Value.z = (newlength * length.value > 0) ? newlength : 0;
            }).ScheduleParallel();

            //Arc and trace segments
            Entities
                .WithNone<Translation>()
                .ForEach(
                    (ref LocalToWorld lcwMatrix, in FloorPosition floorPosition, in TimingGroup group, in BaseShear baseshear, in BaseOffset baseoffset, in Cutoff cutoff, in ChartTime chartTime) =>
                    {
                        float newfp = floorPosition.value - currentFloorPosition[group.value];
                        float percentage = newfp / baseshear.value.z;

                        if (currentTime > chartTime.value && percentage > 0 && cutoff.value)
                        {
                            if (percentage > 1) percentage = 1;
                            float4 shear = baseshear.value * (1 - percentage);
                            float4 offset = baseoffset.value - baseshear.value * percentage;

                            lcwMatrix.Value.c2.x = shear.x;
                            lcwMatrix.Value.c2.y = shear.y;
                            lcwMatrix.Value.c2.z = shear.z;
                            lcwMatrix.Value.c3.x = offset.x;
                            lcwMatrix.Value.c3.y = offset.y;
                            lcwMatrix.Value.c3.z = 0;
                        }
                        else
                            lcwMatrix.Value.c3.z = newfp;
                    }).ScheduleParallel();
        }
    }
}