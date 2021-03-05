using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using ArcCore.Data;
using ArcCore.MonoBehaviours;
using ArcCore.Structs;

public class MoveNotesTowardScreen : SystemBase
{
    protected override void OnUpdate()
    {
        NativeArray<fixed_dec> currentFloorPosition = Conductor.Instance.currentFloorPosition;

        //All note except arcs
        Entities.ForEach((ref Translation translation, in FloorPosition floorPosition, in TimingGroup group) => {
            translation.Value.z = (floorPosition.Value - currentFloorPosition[group.Value]) / -1300; 
        }).Schedule();

        //Arc segments
        Entities.WithNone<Translation>().
            ForEach((ref LocalToWorld lcwMatrix, in FloorPosition floorPosition, in TimingGroup group) => {

                lcwMatrix.Value.c3.z = (floorPosition.Value - currentFloorPosition[group.Value]) / -1300;
            
            }).Schedule();
    }
}
