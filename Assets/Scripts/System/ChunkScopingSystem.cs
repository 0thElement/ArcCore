using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using ArcCore.Data;
using ArcCore.MonoBehaviours;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class ChunkScopingSystem : SystemBase
{
    private int currentTime;
    private EntityQueryDesc makeAppearQueryDesc;
    private EntityQuery makeAppearQuery;
    private EntityQuery makeDisappearQuery;
    private EntityManager entityManager;
    private bool firstFrame = true;
    private EntityQueryDesc appearTimeChangedQueryDesc;
    private EntityQuery appearTimeChangedQuery;
    protected override void OnCreate()
    {
        makeAppearQueryDesc = new EntityQueryDesc
        {
            None = new ComponentType[] {ComponentType.ChunkComponent<ChunkWithinRenderRangeTag>(), typeof(Prefab)},
            All = new ComponentType[] {ComponentType.ChunkComponentReadOnly<ChunkAppearTime>(), typeof(Disabled)}
        };
        makeAppearQuery = GetEntityQuery(makeAppearQueryDesc);
        makeDisappearQuery = GetEntityQuery(ComponentType.ChunkComponent<ChunkDisappearTime>(), 
                                            ComponentType.ChunkComponent<ChunkWithinRenderRangeTag>());

        appearTimeChangedQueryDesc = new EntityQueryDesc() 
        {
            None = new ComponentType[] {ComponentType.ChunkComponent<ChunkWithinRenderRangeTag>(), typeof(Prefab)},
            All = new ComponentType[] {ComponentType.ChunkComponent<ChunkAppearTime>(), typeof(Disabled), typeof(AppearTime)}
        };
        appearTimeChangedQuery = GetEntityQuery(appearTimeChangedQueryDesc);
        appearTimeChangedQuery.SetChangedVersionFilter(typeof(AppearTime));

        entityManager = World.EntityManager;
    }
    protected override void OnStartRunning()
    {
        SetupChunkComponentData();
    }

    private void SetupChunkComponentData()
    {
        EntityQueryDesc arcQueryDesc = new EntityQueryDesc(){
            All = new ComponentType[]{typeof(AppearTime), typeof(DisappearTime)},
            Options = EntityQueryOptions.IncludeDisabled
        };
        EntityQuery arcQuery = entityManager.CreateEntityQuery(new EntityQueryDesc[]{arcQueryDesc});
        NativeArray<ArchetypeChunk> chunks = arcQuery.CreateArchetypeChunkArray(Allocator.Persistent);

        foreach (var chunk in chunks)
        {
            var appearTimeType = entityManager.GetArchetypeChunkComponentType<AppearTime>(true);
            NativeArray<AppearTime> appearTimeList = chunk.GetNativeArray<AppearTime>(appearTimeType);

            int min = appearTimeList[0].Value;
            foreach (AppearTime appearTime in appearTimeList)
            {
                if (min > appearTime.Value) min = appearTime.Value;
            }

            var chunkAppearTimeType = entityManager.GetArchetypeChunkComponentType<ChunkAppearTime>(false);
            chunk.SetChunkComponentData<ChunkAppearTime>(chunkAppearTimeType, new ChunkAppearTime(){
                Value = min
            });

            var disappearTimeType = entityManager.GetArchetypeChunkComponentType<DisappearTime>(true);
            NativeArray<DisappearTime> disappearTimeList = chunk.GetNativeArray<DisappearTime>(disappearTimeType);

            int max = appearTimeList[0].Value;
            foreach (DisappearTime disappearTime in disappearTimeList)
            {
                if (max < disappearTime.Value) max = disappearTime.Value;
            }

            var chunkDisappearTimeType = entityManager.GetArchetypeChunkComponentType<ChunkDisappearTime>(false);
            chunk.SetChunkComponentData<ChunkDisappearTime>(chunkDisappearTimeType, new ChunkDisappearTime(){
                Value = max
            });
        }
        chunks.Dispose();
    }
    protected override void OnUpdate()
    {
        //No jobs in this system since all this does is remove and add tags (which jobs aren't designed for)
        currentTime = Conductor.Instance.receptorTime;
        MakeNoteChunksAppear();
        // MakeNotesChunksDisappear();
    }
    private void MakeNoteChunksAppear()
    {
        //Hacky chunkcomponent assigning

        //Chunk iteration
        NativeArray<ArchetypeChunk> chunks = makeAppearQuery.CreateArchetypeChunkArray(Allocator.TempJob);
        for (int i=0; i<chunks.Length; i++)
        {
            int appearTime = entityManager.GetChunkComponentData<ChunkAppearTime>(chunks[i]).Value;

            if (currentTime >= appearTime)
            {
                //Set write access flag
                var acct = entityManager.GetArchetypeChunkComponentType<AppearTime>(false);
                chunks[i].GetNativeArray(acct); 
            }
        }

        //Delete disabled tag from all chunks that was flagged
        if (!firstFrame) entityManager.RemoveComponent<Disabled>(appearTimeChangedQuery);
        chunks.Dispose();
        firstFrame = false;
    }
}