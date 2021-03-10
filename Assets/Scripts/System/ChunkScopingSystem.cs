using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using ArcCore.Data;
using ArcCore.MonoBehaviours;
using UnityEngine;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class ChunkScopingSystem : SystemBase
{
    private int currentTime;
    private EntityQuery makeAppearQuery;
    private EntityQuery makeDisappearQuery;
    private ArchetypeChunkComponentType<ChunkAppearTime> chunkAppearTimeType;
    private ArchetypeChunkComponentType<ChunkDisappearTime> chunkDisappearTimeType;
    private ArchetypeChunkEntityType entityType;
    private EntityManager entityManager;
    protected override void OnCreate()
    {
        var makeAppearQueryDesc = new EntityQueryDesc
        {
            None = new ComponentType[] {ComponentType.ChunkComponent<ChunkWithinRenderRangeTag>()},
            All = new ComponentType[] {ComponentType.ChunkComponent<ChunkAppearTime>()}
        };
        makeAppearQuery = GetEntityQuery(makeAppearQueryDesc);

        makeDisappearQuery = GetEntityQuery(ComponentType.ChunkComponent<ChunkDisappearTime>(), 
                                            ComponentType.ChunkComponent<ChunkWithinRenderRangeTag>());

        chunkAppearTimeType = GetArchetypeChunkComponentType<ChunkAppearTime>(true);
        chunkDisappearTimeType = GetArchetypeChunkComponentType<ChunkDisappearTime>(true);
        entityType = GetArchetypeChunkEntityType();
        entityManager = World.EntityManager;
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
        NativeArray<ArchetypeChunk> chunks = makeAppearQuery.CreateArchetypeChunkArray(Allocator.TempJob);
        NativeArray<Entity> toMakeAppearChunkEntities = new NativeArray<Entity>(chunks.Length, Allocator.TempJob, NativeArrayOptions.ClearMemory);
        int chunkCount = 0;

        for (int i=0; i<chunks.Length; i++)
        {
            int appearTime = entityManager.GetChunkComponentData<ChunkAppearTime>(chunks[i]).Value;
            if (currentTime >= appearTime)
            {
                entityType = GetArchetypeChunkEntityType();
                NativeArray<Entity> chunkEntities = chunks[i].GetNativeArray(entityType);
                toMakeAppearChunkEntities[chunkCount++] = chunkEntities[0];
            }
        }

        for (int i=0; i<chunkCount; i++)
            entityManager.AddChunkComponentData<ChunkWithinRenderRangeTag>(toMakeAppearChunkEntities[i]);
        chunks.Dispose();
        toMakeAppearChunkEntities.Dispose();
    }
}