using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using ArcCore.Data;
using ArcCore.MonoBehaviours;

public class TagNotes : SystemBase
{
    protected override void OnUpdate()
    {
        Entities.WithAll<LocalToWorld>().ForEach((Entity entity, int entityID) => {
            


        }).Schedule();
    }
}