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
using ArcCore.Gameplay.Systems.Judgement;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(CustomTransformSystemGroup)), UpdateAfter(typeof(MovingNotesSystem))]
    public class MovingIndicatorsSystem : SystemBase
    {
        public struct IndicatorMovementCommand
        {
            public int groupID;
            public float3 position;
        }

        NativeQueue<IndicatorMovementCommand> queue;
        protected override void OnCreate()
        {
            queue = new NativeQueue<IndicatorMovementCommand>(Allocator.Persistent);
        }
        protected override void OnUpdate()
        {
            if (!PlayManager.IsUpdatingAndActive) return;

            int currentTime = PlayManager.ReceptorTime;
            var parallel = queue.AsParallelWriter();

            //Arc and trace segments
            JobHandle arcBodies = Entities
                .WithNone<Translation, IsTraceBodies>()
                .ForEach(
                    (in ArcGroupID groupID, in LocalToWorld lcw, in ChartTime chartTime, in ChartEndTime endTime, in Cutoff cutoff) =>
                    {
                        if (currentTime >= chartTime.value && currentTime <= endTime.value)
                        {
                            float3 pos;

                            if (cutoff.value)
                                pos = new float3(lcw.Value.c3.x, lcw.Value.c3.y, 0);
                            else
                            {
                                float dx = lcw.Value.c2.x;
                                float dy = lcw.Value.c2.y;
                                float dz = lcw.Value.c2.z;

                                float sx = lcw.Value.c3.x;
                                float sy = lcw.Value.c3.y;
                                float sz = lcw.Value.c3.z;

                                float x = -sz * dx / dz + sx;
                                float y = -sz * dy / dz + sy;
                                pos = new float3(x, y, 0);
                            }

                            parallel.Enqueue(
                                new IndicatorMovementCommand{
                                    groupID = groupID.value,
                                    position = pos
                                });
                        }
                    }).ScheduleParallel(this.Dependency);

            var dependency = JobHandle.CombineDependencies(arcBodies, this.Dependency);

            JobHandle arcHeads = Entities
                .ForEach(
                    (in ArcGroupID groupID, in Translation translation) =>
                    {
                        parallel.Enqueue(
                            new IndicatorMovementCommand{
                                groupID = groupID.value,
                                position = translation.Value
                            }
                        );
                }).ScheduleParallel(dependency);

            arcBodies.Complete();
            arcHeads.Complete();

            while (queue.Count > 0)
            {
                var command = queue.Dequeue();
                PlayManager.ArcIndicatorHandler.GetIndicator(command.groupID).Update(command.position);
            }
            PlayManager.ArcIndicatorHandler.CheckForDisable();
            
            Entities
                .WithAll<IsTraceBodies>()
                .WithNone<Translation>()
                .ForEach(
                    (in ArcGroupID groupID, in LocalToWorld lcw, in ChartTime chartTime, in ChartEndTime endTime) =>
                    {
                        if (currentTime >= chartTime.value && currentTime <= endTime.value)
                        {
                            parallel.Enqueue(
                                new IndicatorMovementCommand{
                                    groupID = groupID.value,
                                    position = new float3(lcw.Value.c3.x, lcw.Value.c3.y, 0)
                                });
                        }
                    }).ScheduleParallel(this.Dependency).Complete();

            while (queue.Count > 0)
            {
                var command = queue.Dequeue();
                PlayManager.TraceIndicatorHandler.GetIndicator(command.groupID).Update(command.position);
            }
            PlayManager.TraceIndicatorHandler.CheckForDisable();
        }

        protected override void OnDestroy()
        {
            queue.Dispose();
        }
    }
}