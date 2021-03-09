using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using ArcCore.Data;
using ArcCore.MonoBehaviours;
using ArcCore.Tags;
using Unity.Rendering;

public class JudgementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        NativeArray<TouchPoint> touchPoints = InputManager.Instance.touchPoints;

        //SETUP LOCAL
        var job = Entities.WithAll<ChartTime, EntityReference>().WithAny<Track, SinglePosition, PositionPair>().ForEach(
            () =>
            {

            });
    }
}
