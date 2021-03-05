using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class MoveArcsTowardScreen : SystemBase
{
    protected override void OnUpdate()
    {
        float currentFloorPosition = Conductor.Instance.currentFloorPosition[0];
        Entities.WithNone<Translation>().
            ForEach((ref LocalToWorld lcwMatrix, in FloorPosition floorPosition) => {

                lcwMatrix.Value.c3.z = floorPosition.Value - currentFloorPosition;
            
            }).Schedule();
    }
}
