using System.Collections.Generic;
using ArcCore.Gameplay.Objects.Particle;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using ArcCore.Gameplay.Components;
using ArcCore.Parsing.Data;
using ArcCore.Utilities.Extensions;
using ArcCore.Parsing;
using ArcCore.Gameplay.Data;

namespace ArcCore.Gameplay.EntityCreation
{
    public class ArcEntityCreator : ArclikeEntityCreator
    {
        private Material arcMaterial;
        private Material heightMaterial;
        private Mesh arcMesh;
        private Mesh headMesh;
        private Mesh heightMesh;
        private RenderMesh shadowRenderMesh;
        private RenderMesh shadowGrayoutRenderMesh;

        private Entity arcNoteEntityPrefab;
        private Entity headArcNoteEntityPrefab;
        private Entity heightIndicatorEntityPrefab;
        private Entity arcShadowEntityPrefab;

        private GameObject arcApproachIndicatorPrefab;
        private GameObject arcParticlePrefab;

        private EntityArchetype arcJudgeArchetype;

        private int colorShaderId;
        private int highlightShaderId;

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
                //Judge time
                ComponentType.ReadWrite<ChartIncrTime>(),
                //Color
                ComponentType.ReadOnly<ArcColorID>(),
                //Arc data
                ComponentType.ReadOnly<ArcData>(),
                ComponentType.ReadOnly<ArcGroupID>()
            );

            //Shader ID
            colorShaderId     = Shader.PropertyToID("_Color");
            highlightShaderId = Shader.PropertyToID("_Highlight");

            //Extract material and mesh from prefab object
            arcMaterial    = arcNotePrefab.        GetComponent<Renderer>().sharedMaterial;
            heightMaterial = heightIndicatorPrefab.GetComponent<Renderer>().sharedMaterial;

            arcMesh    = arcNotePrefab.        GetComponent<MeshFilter>().sharedMesh;
            headMesh   = headArcNotePrefab.    GetComponent<MeshFilter>().sharedMesh;
            heightMesh = heightIndicatorPrefab.GetComponent<MeshFilter>().sharedMesh;

            shadowRenderMesh = em.GetSharedComponentData<RenderMesh>(arcShadowEntityPrefab);

            Material shadowMaterial        = shadowRenderMesh.material;
            Material shadowGrayoutMaterial = Object.Instantiate(shadowMaterial);

            shadowMaterial       .SetFloat(highlightShaderId, 1);
            shadowGrayoutMaterial.SetFloat(highlightShaderId, -1);

            shadowGrayoutRenderMesh = new RenderMesh()
            {
                mesh = shadowRenderMesh.mesh,
                material = shadowGrayoutMaterial
            };
        }

        public void CreateEntitiesAndGetData(
            IChartParser parser,
            out List<RenderMesh> initial,
            out List<RenderMesh> highlight,
            out List<RenderMesh> grayout,
            out List<RenderMesh> head,
            out List<RenderMesh> height,
            out RenderMesh shadow,
            out RenderMesh shadowGrayout,
            out int arcGroupCount)
        {
            //SETUP MATERIALS
            initial   = new List<RenderMesh>();
            highlight = new List<RenderMesh>();
            grayout   = new List<RenderMesh>();
            head      = new List<RenderMesh>();
            height    = new List<RenderMesh>();

            for (int i = 0; i <= parser.MaxArcColor; i++)
            {
                Material arcColorMaterialInstance             = Object.Instantiate(arcMaterial);
                Material heightIndicatorColorMaterialInstance = Object.Instantiate(heightMaterial);

                arcColorMaterialInstance            .SetColor(colorShaderId, UserSettings.Instance.GetArcColor(i));
                heightIndicatorColorMaterialInstance.SetColor(colorShaderId, UserSettings.Instance.GetArcColor(i));

                Material highlightMat = Object.Instantiate(arcColorMaterialInstance);
                Material grayoutMat   = Object.Instantiate(arcColorMaterialInstance);

                highlightMat.SetFloat(highlightShaderId, 1);
                grayoutMat  .SetFloat(highlightShaderId,-1);

                initial  .Add(new RenderMesh { mesh = arcMesh   , material = arcColorMaterialInstance });
                highlight.Add(new RenderMesh { mesh = arcMesh   , material = highlightMat });
                grayout  .Add(new RenderMesh { mesh = arcMesh   , material = grayoutMat });
                head     .Add(new RenderMesh { mesh = headMesh  , material = arcColorMaterialInstance });
                height   .Add(new RenderMesh { mesh = heightMesh, material = heightIndicatorColorMaterialInstance });
            }

            shadow        = shadowRenderMesh;
            shadowGrayout = shadowGrayoutRenderMesh;

            arcGroupCount = CreateArclike(parser.Arcs);
        }

        protected override void SetupIndicators(List<ArcPointData> connectedArcsIdEndpoint)
        {
            List<IIndicator> indicatorList = new List<IIndicator>(connectedArcsIdEndpoint.Count);

            foreach (var groupIdEndPoint in connectedArcsIdEndpoint)
            {
                ArcIndicator indicator = new ArcIndicator(
                    Object.Instantiate(arcApproachIndicatorPrefab),
                    Object.Instantiate(arcParticlePrefab),
                    groupIdEndPoint.time + 60
                );
                indicatorList.Add(indicator);
            }
            PlayManager.ArcIndicatorHandler.Initialize(indicatorList);
        }

        protected override void CreateSegment(ArcRaw arc, float3 start, float3 end, int timing, int endTiming, int groupId)
        {
            Entity arcInstEntity = em.Instantiate(arcNoteEntityPrefab);

            RenderMesh renderMesh = PlayManager.ArcInitialRenderMeshes[arc.color];
            em.SetSharedComponentData<RenderMesh>(arcInstEntity, renderMesh); 

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

            if (timing < endTiming)
            {
                Entity arcShadowEntity = em.Instantiate(arcShadowEntityPrefab);
                em.SetSharedComponentData<RenderMesh>(arcShadowEntity, PlayManager.ArcShadowRenderMesh);
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
            }
        }

        protected override void CreateHeightIndicator(ArcRaw arc)
        {
            Entity heightEntity = em.Instantiate(heightIndicatorEntityPrefab);

            RenderMesh renderMesh = PlayManager.ArcHeightRenderMeshes[arc.color];
            em.SetSharedComponentData(heightEntity, renderMesh);

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
        }

        protected override void CreateHeadSegment(ArcRaw arc, int groupID)
        {
            Entity headEntity = em.Instantiate(headArcNoteEntityPrefab);

            RenderMesh renderMesh = PlayManager.ArcheadRenderMeshes[arc.color];
            em.SetSharedComponentData(headEntity, renderMesh);

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
        }

        protected override void CreateJudgeEntity(ArcRaw arc, int groupId, float startBpm)
        {

            Entity en = em.CreateEntity(arcJudgeArchetype);

            //very stupid
            em.SetComponentData(en, new ChartTime(arc.timing + Constants.LostWindow));
            em.SetSharedComponentData(en, new ArcColorID(arc.color));
            em.SetComponentData(en, new ArcData(arc));
            em.SetComponentData(en, new ArcGroupID(groupId));
            em.SetComponentData(en, new DestroyOnTiming(arc.endTiming + Constants.HoldLostWindow));

            em.SetComponentData(en, ChartIncrTime.FromBpm(arc.timing, arc.endTiming, startBpm, out int comboCount));
            
            PlayManager.ScoreHandler.tracker.noteCount += comboCount;
        }
    }
}