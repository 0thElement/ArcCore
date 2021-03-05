using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class MoveNotesTowardScreen : SystemBase
{
    protected override void OnUpdate()
    {
        float currentFloorPosition = Conductor.Instance.currentFloorPosition[0];
        Entities.ForEach((ref Translation translation, in FloorPosition floorPosition) => {
            translation.Value.z = floorPosition.Value - currentFloorPosition; 
        }).Schedule();
    }
}
