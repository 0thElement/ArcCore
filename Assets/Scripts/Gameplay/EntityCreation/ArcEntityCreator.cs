using System.Collections.Generic;
using ArcCore.Gameplay.Objects.Particle;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using ArcCore.Gameplay.Parsing.Data;
using ArcCore.Gameplay.Parsing;
using ArcCore.Gameplay.Data;
using ArcCore.Utilities;
using ArcCore.Gameplay.Utilities;

namespace ArcCore.Gameplay.EntityCreation
{
    public class ArcEntityCreator : ArclikeEntityCreator
    {
        private Mesh arcMesh;
        private Mesh heightMesh;
        private Mesh shadowMesh;
        private Mesh headMesh;
        private Entity arcNoteEntityPrefab;
        private Entity headArcNoteEntityPrefab;
        private Entity heightIndicatorEntityPrefab;
        private Entity arcShadowEntityPrefab;
        private ScopingChunk arcNoteScopingChunk;
        private ScopingChunk headArcNoteScopingChunk;
        private ScopingChunk heightIndicatorScopingChunk;
        private ScopingChunk arcShadowScopingChunk;
        private ScopingChunk arcJudgeScopingChunk;

        private GameObject arcApproachIndicatorPrefab;
        private GameObject arcParticlePrefab;

        private EntityArchetype arcJudgeArchetype;

        private EntityManager em;

        public ArcEntityCreator(
            World world,
            GameObject arcNotePrefab,
            GameObject headArcNotePrefab,
            GameObject heightIndicatorPrefab,
            GameObject arcShadowPrefab,
            GameObject arcApproachIndicatorPrefab,
            GameObject arcParticlePrefab
            )
        {
            em = world.EntityManager;
            var gocs = GameObjectConversionSettings.FromWorld(world, null);

            this.arcApproachIndicatorPrefab = arcApproachIndicatorPrefab;
            this.arcParticlePrefab = arcParticlePrefab;

            //Entity prefabs conversion
            arcNoteEntityPrefab         = gocs.ConvertToNote(arcNotePrefab, em);
            headArcNoteEntityPrefab     = gocs.ConvertToNote(headArcNotePrefab, em);
            heightIndicatorEntityPrefab = gocs.ConvertToNote(heightIndicatorPrefab, em);
            arcShadowEntityPrefab       = gocs.ConvertToNote(arcShadowPrefab, em);

            em.ExposeLocalToWorld(arcNoteEntityPrefab);
            em.ExposeLocalToWorld(arcShadowEntityPrefab);

            arcJudgeArchetype = em.CreateArchetype(
                
                //Chart time
                ComponentType.ReadOnly<ChartTime>(),
                ComponentType.ReadOnly<DestroyOnTiming>(),
                ComponentType.ReadOnly<ChunkAppearTime>(),
                //Judge time
                ComponentType.ReadWrite<ChartIncrTime>(),
                //Color
                ComponentType.ReadOnly<ArcColorID>(),
                //Arc data
                ComponentType.ReadOnly<ArcData>(),
                ComponentType.ReadOnly<ArcGroupID>()
            );

            arcMesh    = arcNotePrefab.        GetComponent<MeshFilter>().sharedMesh;
            headMesh   = headArcNotePrefab.    GetComponent<MeshFilter>().sharedMesh;
            heightMesh = heightIndicatorPrefab.GetComponent<MeshFilter>().sharedMesh;
            shadowMesh = arcShadowPrefab.      GetComponent<MeshFilter>().sharedMesh;

            arcNoteScopingChunk = new ScopingChunk(em.GetChunk(arcNoteEntityPrefab).Archetype.ChunkCapacity);
            headArcNoteScopingChunk = new ScopingChunk(em.GetChunk(headArcNoteEntityPrefab).Archetype.ChunkCapacity);
            heightIndicatorScopingChunk = new ScopingChunk(em.GetChunk(heightIndicatorEntityPrefab).Archetype.ChunkCapacity);
            arcShadowScopingChunk = new ScopingChunk(em.GetChunk(arcShadowEntityPrefab).Archetype.ChunkCapacity);
            arcJudgeScopingChunk = new ScopingChunk(arcJudgeArchetype.ChunkCapacity);
        }

        public void CreateEntities(IChartParser parser, out int arcGroupCount)
        {
            arcGroupCount = CreateArclike(parser, parser.Arcs);
        }

        protected override void SetupIndicators(List<ArcPointData> connectedArcsIdEndpoint)
        {
            List<IIndicator> indicatorList = new List<IIndicator>(connectedArcsIdEndpoint.Count);

            foreach (var groupIdEndPoint in connectedArcsIdEndpoint)
            {
                ArcIndicator indicator = new ArcIndicator(
                    Object.Instantiate(arcApproachIndicatorPrefab, PlayManager.IndicatorParent),
                    Object.Instantiate(arcParticlePrefab, PlayManager.IndicatorParent),
                    groupIdEndPoint.time,
                    groupIdEndPoint.color
                );
                indicatorList.Add(indicator);
            }
            PlayManager.ArcIndicatorHandler.Initialize(indicatorList);
        }

        protected override void CreateSegment(ArcRaw arc, float3 start, float3 end, int timing, int endTiming, int groupId, TimingGroupFlag flag)
        {
            Entity arcInstEntity = em.Instantiate(arcNoteEntityPrefab);

            em.SetSharedComponentData<RenderMesh>(arcInstEntity, Skin.Instance.arcInitialRenderMeshes[arc.color]); 

            float dx = start.x - end.x;
            float dy = start.y - end.y;
            float dz = start.z - end.z;


            int t1 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(start.z + Constants.RenderFloorPositionRange, arc.timingGroup);
            int t2 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(end.z - Constants.RenderFloorPositionRange, arc.timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;

            //Shear along xy + scale along z matrix
            LocalToWorld ltwArc = new LocalToWorld()
            {
                Value = new float4x4(
                    1, 0, dx, start.x,
                    0, 1, dy, start.y,
                    0, 0, dz, 0,
                    0, 0, 0, 1
                )
            };
            em.SetComponentData(arcInstEntity, ltwArc);

            em.SetComponentData(arcInstEntity, new FloorPosition(start.z));
            em.SetComponentData(arcInstEntity, new TimingGroup(arc.timingGroup));
            em.SetComponentData(arcInstEntity, new BaseOffset(new float4(start.x, start.y, 0, 0)));
            em.SetComponentData(arcInstEntity, new BaseShear(new float4(dx, dy, dz, 0)));
            em.SetComponentData(arcInstEntity, new Cutoff(false));
            em.SetComponentData(arcInstEntity, new AppearTime(appearTime));
            em.SetComponentData(arcInstEntity, new DestroyOnTiming(endTiming + Constants.HoldLostWindow));
            em.SetComponentData(arcInstEntity, new ArcGroupID(groupId));
            em.SetComponentData(arcInstEntity, new ChartTime(timing));
            em.SetComponentData(arcInstEntity, new ChartEndTime(endTiming));

            em.SetSharedComponentData(arcInstEntity, new ChunkAppearTime(arcNoteScopingChunk.AddAppearTiming(appearTime)));

            if (timing < endTiming && !flag.HasFlag(TimingGroupFlag.NoShadow))
            {
                Entity arcShadowEntity = em.Instantiate(arcShadowEntityPrefab);
                em.SetSharedComponentData<RenderMesh>(arcShadowEntity, Skin.Instance.arcShadowRenderMesh);
                LocalToWorld ltwShadow = new LocalToWorld()
                {
                    Value = new float4x4(
                        1, 0, dx, start.x,
                        0, 1, 0, 0,
                        0, 0, dz, 0,
                        0, 0, 0, 1
                    )
                };
                em.SetComponentData(arcShadowEntity, ltwShadow);

                em.SetComponentData(arcShadowEntity, new FloorPosition(start.z));
                em.SetComponentData(arcShadowEntity, new TimingGroup(arc.timingGroup));
                em.SetComponentData(arcShadowEntity, new BaseOffset(new float4(start.x, 0, 0, 0)));
                em.SetComponentData(arcShadowEntity, new BaseShear(new float4(dx, 0, dz, 0)));
                em.SetComponentData(arcShadowEntity, new Cutoff(false));
                em.SetComponentData(arcShadowEntity, new AppearTime(appearTime));
                em.SetComponentData(arcShadowEntity, new DestroyOnTiming(endTiming + Constants.HoldLostWindow));
                em.SetComponentData(arcShadowEntity, new ArcGroupID(groupId));
                em.SetComponentData(arcShadowEntity, new ChartTime(timing));

                em.SetSharedComponentData(arcShadowEntity, new ChunkAppearTime(arcShadowScopingChunk.AddAppearTiming(appearTime)));
            }
        }

        protected override void CreateHeightIndicator(ArcRaw arc, TimingGroupFlag flag)
        {
            if (flag.HasFlag(TimingGroupFlag.NoHeightIndicator)) return;

            Entity heightEntity = em.Instantiate(heightIndicatorEntityPrefab);

            em.SetSharedComponentData(heightEntity, Skin.Instance.arcHeightRenderMeshes[arc.color]);

            float floorpos = PlayManager.Conductor.GetFloorPositionFromTiming(arc.timing, arc.timingGroup);


            float height = Conversion.GetWorldY(arc.startY) - 0.45f;


            float x = Conversion.GetWorldX(arc.startX); 
            float y = height / 2;
            const float z = 0;

            const float scaleX = 2.34f;
            float scaleY = height;
            const float scaleZ = 1;

            int t1 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, arc.timingGroup);
            int t2 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, arc.timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;

            em.SetComponentData(heightEntity, new Translation() { Value = new float3(x, y, z) });
            em.AddComponentData(heightEntity, new NonUniformScale() { Value = new float3(scaleX, scaleY, scaleZ) });
            em.AddComponentData(heightEntity, new FloorPosition(floorpos));
            em.SetComponentData(heightEntity, new TimingGroup(arc.timingGroup));
            em.SetComponentData(heightEntity, new AppearTime(appearTime));
            em.SetComponentData(heightEntity, new DestroyOnTiming(arc.timing));

            em.SetSharedComponentData(heightEntity, new ChunkAppearTime(heightIndicatorScopingChunk.AddAppearTiming(appearTime)));
        }

        protected override void CreateHeadSegment(ArcRaw arc, int groupID, TimingGroupFlag flag)
        {
            Entity headEntity = em.Instantiate(headArcNoteEntityPrefab);

            em.SetSharedComponentData(headEntity, Skin.Instance.arcHeadRenderMeshes[arc.color]);

            float floorpos = PlayManager.Conductor.GetFloorPositionFromTiming(arc.timing, arc.timingGroup);


            float x = Conversion.GetWorldX(arc.startX); 

            float y = Conversion.GetWorldY(arc.startY); 
            const float z = 0;

            int t1 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, arc.timingGroup);
            int t2 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, arc.timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;

            em.SetComponentData(headEntity, new Translation() { Value = math.float3(x, y, z) });
            em.SetComponentData(headEntity, new FloorPosition(floorpos));
            em.SetComponentData(headEntity, new TimingGroup(arc.timingGroup));
            em.SetComponentData(headEntity, new AppearTime(appearTime));
            em.SetComponentData(headEntity, new DestroyOnTiming(arc.timing));
            em.SetComponentData(headEntity, new ArcGroupID(groupID));

            em.SetSharedComponentData(headEntity, new ChunkAppearTime(headArcNoteScopingChunk.AddAppearTiming(appearTime)));

            //TODO: Shadow for headseg??
        }

        protected override void CreateJudgeEntity(ArcRaw arc, int groupId, float startBpm, TimingGroupFlag flag)
        {
            if (flag.HasFlag(TimingGroupFlag.NoInput)) return; //I'm not sure if this is going to work

            Entity en = em.CreateEntity(arcJudgeArchetype);

            //very stupid
            em.SetComponentData(en, new ChartTime(arc.timing + Constants.LostWindow));
            em.SetSharedComponentData(en, new ArcColorID(arc.color));
            em.SetComponentData(en, new ArcData(arc));
            em.SetComponentData(en, new ArcGroupID(groupId));
            em.SetComponentData(en, new DestroyOnTiming(arc.endTiming + Constants.HoldLostWindow));

            em.SetSharedComponentData(en, new ChunkAppearTime(arcJudgeScopingChunk.AddAppearTiming(arc.timing)));

            em.SetComponentData(en, ChartIncrTime.FromBpm(arc.timing, arc.endTiming, startBpm, out int comboCount));

            if (flag.HasFlag(TimingGroupFlag.Autoplay))
                em.AddComponent(en, typeof(Autoplay));
            
            PlayManager.ScoreHandler.tracker.noteCount += comboCount;
        }
    }
}