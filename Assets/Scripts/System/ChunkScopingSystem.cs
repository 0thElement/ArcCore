using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using ArcCore.Components;
using ArcCore.Behaviours;
using ArcCore.Components.Chunk;
using ArcCore.Components.Tags;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class ChunkScopingSystem : SystemBase
{
    private int currentTime;
    private int nextMakeAppearUpdateTime = int.MinValue;
    private int nextMakeDisappearUpdateTime = int.MinValue;
    private EntityQueryDesc makeAppearQueryDesc;
    private EntityQuery makeAppearQuery;
    private EntityQuery makeDisappearQuery;
    private EntityManager entityManager;
    private bool firstFrame = true;
    private EntityQuery appearTimeChangedQuery;
    private EntityQuery disappearTimeChangedQuery;

    protected override void OnCreate()
    {
        makeAppearQueryDesc = new EntityQueryDesc
        {
            None = new ComponentType[] {typeof(Disappeared), typeof(Prefab)},
            All = new ComponentType[] {ComponentType.ChunkComponentReadOnly<ChunkAppearTime>(), typeof(Disabled)}
        };
        makeAppearQuery = GetEntityQuery(makeAppearQueryDesc);

        makeDisappearQuery = GetEntityQuery(ComponentType.ChunkComponent<ChunkDisappearTime>());

        var appearTimeChangedQueryDesc = new EntityQueryDesc() 
        {
            None = new ComponentType[] {typeof(Prefab)},
            All = new ComponentType[] {ComponentType.ChunkComponent<ChunkAppearTime>(), typeof(Disabled), typeof(AppearTime)}
        };
        appearTimeChangedQuery = GetEntityQuery(appearTimeChangedQueryDesc);
        appearTimeChangedQuery.SetChangedVersionFilter(typeof(AppearTime));

        var disappearTimeChangedQueryDesc = new EntityQueryDesc() 
        {
            None = new ComponentType[] {typeof(Prefab), typeof(Disabled)},
            All = new ComponentType[] {ComponentType.ChunkComponent<ChunkDisappearTime>(), typeof(DisappearTime)}
        };
        disappearTimeChangedQuery = GetEntityQuery(disappearTimeChangedQueryDesc);
        disappearTimeChangedQuery.SetChangedVersionFilter(typeof(DisappearTime));

        entityManager = World.EntityManager;
    }

    protected override void OnStartRunning()
    {
        SetupChunkComponentData();
    }

    private void SetupChunkComponentData()
    {
        EntityQueryDesc queryDesc = new EntityQueryDesc(){
            All = new ComponentType[]{typeof(AppearTime)},
            Options = EntityQueryOptions.IncludeDisabled
        };
        EntityQuery query = entityManager.CreateEntityQuery(new EntityQueryDesc[]{queryDesc});
        NativeArray<ArchetypeChunk> chunks = query.CreateArchetypeChunkArray(Allocator.Persistent);

        foreach (var chunk in chunks)
        {
            var appearTimeType = entityManager.GetArchetypeChunkComponentType<AppearTime>(true);
            NativeArray<AppearTime> appearTimeList = chunk.GetNativeArray<AppearTime>(appearTimeType);

            int min = appearTimeList[0].value;
            foreach (AppearTime appearTime in appearTimeList)
            {
                if (min > appearTime.value) min = appearTime.value;
            }

            var chunkAppearTimeType = entityManager.GetArchetypeChunkComponentType<ChunkAppearTime>(false);
            chunk.SetChunkComponentData<ChunkAppearTime>(chunkAppearTimeType, new ChunkAppearTime(){
                Value = min
            });
        }
        chunks.Dispose();

        queryDesc = new EntityQueryDesc(){
            All = new ComponentType[]{typeof(DisappearTime)},
            Options = EntityQueryOptions.IncludeDisabled
        };
        query = entityManager.CreateEntityQuery(new EntityQueryDesc[]{queryDesc});
        chunks = query.CreateArchetypeChunkArray(Allocator.Persistent);

        foreach (var chunk in chunks)
        {
            var disappearTimeType = entityManager.GetArchetypeChunkComponentType<DisappearTime>(true);
            NativeArray<DisappearTime> disappearTimeList = chunk.GetNativeArray<DisappearTime>(disappearTimeType);

            int max = disappearTimeList[0].value;
            foreach (DisappearTime disappearTime in disappearTimeList)
            {
                if (max < disappearTime.value) max = disappearTime.value;
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
        if (firstFrame)
        {
            firstFrame = false;
            return;
        }
        currentTime = Conductor.Instance.receptorTime;
        if (currentTime >= nextMakeAppearUpdateTime) MakeNoteChunksAppear();
        if (currentTime >= nextMakeDisappearUpdateTime) MakeNotesChunksDisappear();
    }
    private void MakeNoteChunksAppear()
    {
        //Hacky chunkcomponent assigning

        //Chunk iteration
        NativeArray<ArchetypeChunk> chunks = makeAppearQuery.CreateArchetypeChunkArray(Allocator.TempJob);

        int minNextAppear = int.MaxValue;
        for (int i=0; i<chunks.Length; i++)
        {
            int appearTime = entityManager.GetChunkComponentData<ChunkAppearTime>(chunks[i]).Value;

            if (currentTime >= appearTime)
            {
                //Set write access flag
                var acct = entityManager.GetArchetypeChunkComponentType<AppearTime>(false);
                chunks[i].GetNativeArray(acct); 
            }
            else if (appearTime < minNextAppear) minNextAppear = appearTime;
        }

        //Delete disabled tag from all chunks that was flagged
        entityManager.RemoveComponent<Disabled>(appearTimeChangedQuery);
        chunks.Dispose();

        nextMakeAppearUpdateTime = minNextAppear;
    }
    private void MakeNotesChunksDisappear()
    {
        NativeArray<ArchetypeChunk> chunks = makeDisappearQuery.CreateArchetypeChunkArray(Allocator.TempJob);
        
        int minNextDisappear = int.MaxValue;
        for (int i=0; i<chunks.Length; i++)
        {
            int disappearTime = entityManager.GetChunkComponentData<ChunkDisappearTime>(chunks[i]).Value;

            if (currentTime >= disappearTime)
            {
                var acct = entityManager.GetArchetypeChunkComponentType<DisappearTime>(false);
                chunks[i].GetNativeArray(acct);
            }
            else if (disappearTime < minNextDisappear) minNextDisappear = disappearTime;
        }

        entityManager.AddComponent<Disappeared>(disappearTimeChangedQuery);
        entityManager.AddComponent<Disabled>(disappearTimeChangedQuery);
        chunks.Dispose();

        nextMakeDisappearUpdateTime = minNextDisappear;
    }
}