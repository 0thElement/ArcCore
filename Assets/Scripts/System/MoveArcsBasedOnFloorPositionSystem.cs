using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class MoveArcBasedOnFloorPositionSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float currentFloorPosition = Conductor.Instance.currentFloorPosition[0];
        Entities.ForEach((ref LocalToWorld lcwMatrix, in ArcStartPosition start, in FloorPosition floorPosition) => {

            //the last column correspond to translation
            lcwMatrix.Value.c3.x = start.Value.x;
            lcwMatrix.Value.c3.y = start.Value.y;
            lcwMatrix.Value.c3.z = floorPosition.Value - currentFloorPosition;
            
        }).Schedule();
    }
}
