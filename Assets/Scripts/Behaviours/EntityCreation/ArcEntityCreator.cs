﻿using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using ArcCore.Utility;
using ArcCore.Components;
using ArcCore.Components.Chunk;
using ArcCore.Parsing;
using ArcCore.Structs;

namespace ArcCore.Behaviours.EntityCreation
{
    public class ArcEntityCreator : MonoBehaviour
    {
        public static ArcEntityCreator Instance { get; private set; }
        [SerializeField] private GameObject arcNotePrefab;
        [SerializeField] private GameObject headArcNotePrefab;
        [SerializeField] private GameObject heightIndicatorPrefab;
        [SerializeField] private GameObject arcShadowPrefab;
        [SerializeField] private Material arcMaterial;
        [SerializeField] private Material heightMaterial;
        [SerializeField] public Color[] arcColors;
        [SerializeField] private Color redColor;
        [SerializeField] private Mesh arcMesh;
        [SerializeField] private Mesh headMesh;
        private Entity arcNoteEntityPrefab;
        private Entity headArcNoteEntityPrefab;
        private Entity heightIndicatorEntityPrefab;
        private Entity arcShadowEntityPrefab;
        private World defaultWorld;
        private EntityManager entityManager;
        private int colorShaderId;
        private int redColorShaderId;

        public static int ColorCount => Instance.arcColors.Length;

        /// <summary>
        /// Time between two judge points of a similar area and differing colorIDs in which both points will be set as unscrict
        /// </summary>
        public const int judgeStrictnessLeniency = 100;
        /// <summary>
        /// The distance between two points (in world space) at which they will begin to be considered for unstrictness
        /// </summary>
        public const float judgeStrictnessDist = 1f;

        private void Awake()
        {
            Instance = this;
            defaultWorld = World.DefaultGameObjectInjectionWorld;
            entityManager = defaultWorld.EntityManager;
            GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(defaultWorld, null);
            arcNoteEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(arcNotePrefab, settings);
            headArcNoteEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(headArcNotePrefab, settings);
            heightIndicatorEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(heightIndicatorPrefab, settings);
            arcShadowEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(arcShadowPrefab, settings);
            
            //Remove these component to allow direct access to localtoworld matrices
            //idk if this is a good way to set up an entity prefab in this case but this will do for now
            entityManager.RemoveComponent<Translation>(arcNoteEntityPrefab);
            entityManager.RemoveComponent<Rotation>(arcNoteEntityPrefab);
            entityManager.AddComponent<Disabled>(arcNoteEntityPrefab);
            entityManager.AddChunkComponentData<ChunkAppearTime>(arcNoteEntityPrefab);
            entityManager.AddChunkComponentData<ChunkDisappearTime>(arcNoteEntityPrefab);
            
            entityManager.RemoveComponent<Translation>(arcShadowEntityPrefab);
            entityManager.RemoveComponent<Rotation>(arcShadowEntityPrefab);

            entityManager.AddComponent<Disabled>(headArcNoteEntityPrefab);
            entityManager.AddChunkComponentData<ChunkAppearTime>(headArcNoteEntityPrefab);
            
            entityManager.AddComponent<Disabled>(heightIndicatorEntityPrefab);
            entityManager.AddChunkComponentData<ChunkAppearTime>(heightIndicatorEntityPrefab);


            colorShaderId = Shader.PropertyToID("_Color");
            redColorShaderId = Shader.PropertyToID("_RedCol");

            JudgementSystem.Instance.SetupColors();
        }

        public void CreateEntities(List<List<AffArc>> affArcList)
        {
            int colorId=0;

            //SET UP NEW JUDGES HEREEEEE
            List<List<ArcJudge>> judges = new List<List<ArcJudge>>();
            List<AffArc> rawArcs = new List<AffArc>();

            foreach (List<AffArc> listByColor in affArcList)
            {
                listByColor.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });

                Material arcColorMaterialInstance = Instantiate(arcMaterial);
                Material heightIndicatorColorMaterialInstance = Instantiate(heightMaterial);
                arcColorMaterialInstance.SetColor(colorShaderId, arcColors[colorId]);
                arcColorMaterialInstance.SetColor(redColorShaderId, redColor);
                heightIndicatorColorMaterialInstance.SetColor(colorShaderId, arcColors[colorId]);

                List<float4> connectedArcsIdEndpoint = new List<float4>();

                foreach (AffArc arc in listByColor)
                {
                    rawArcs.Add(arc);

                    //Precalc and assign a connected arc id to avoid having to figure out connection during gameplay
                    //this is really dumb but i don't want to split this into another class
                    float4 arcStartPoint = new float4((float)arc.timingGroup, (float)arc.timing, arc.startX, arc.startY);
                    float4 arcEndPoint = new float4((float)arc.timingGroup, (float)arc.endTiming, arc.endX, arc.endY);
                    int arcId = -1;
                    bool isHeadArc = true;
                    for (int id = 0; id < connectedArcsIdEndpoint.Count; id++)
                    {
                        if (connectedArcsIdEndpoint[id].Equals(arcStartPoint))
                        {
                            arcId = id;
                            isHeadArc = false;
                            connectedArcsIdEndpoint[id] = arcEndPoint;
                        }
                    }

                    if (isHeadArc)
                    {
                        arcId = connectedArcsIdEndpoint.Count;
                        connectedArcsIdEndpoint.Add(arcEndPoint);
                        CreateHeadSegment(arc, arcColorMaterialInstance);
                    }
                    if (isHeadArc || arc.startY != arc.endY)
                        CreateHeightIndicator(arc, heightIndicatorColorMaterialInstance);

                    //Generate arc segments and shadow segment(each segment is its own entity)
                    int duration = arc.endTiming - arc.timing;
                    int v1 = duration < 1000 ? 14 : 7;
                    float v2 = 1f / (v1 * duration / 1000f);
                    float segmentLength = duration * v2;
                    int segmentCount = (int)(segmentLength == 0 ? 0 : duration / segmentLength) + 1;

                    float3 start;
                    float3 end = new float3(
                        Conversion.GetWorldX(arc.startX),
                        Conversion.GetWorldY(arc.startY),
                        Conductor.Instance.GetFloorPositionFromTiming(arc.timing, arc.timingGroup)
                    );

                    for (int i = 0; i < segmentCount - 1; i++)
                    {
                        float t = (i + 1) * segmentLength;
                        start = end;
                        end = new float3(
                            Conversion.GetWorldX(Conversion.GetXAt(t / duration, arc.startX, arc.endX, arc.easing)),
                            Conversion.GetWorldY(Conversion.GetYAt(t / duration, arc.startY, arc.endY, arc.easing)),
                            Conductor.Instance.GetFloorPositionFromTiming((int)(arc.timing + t), arc.timingGroup)
                        );

                        CreateSegment(arcColorMaterialInstance, start, end, arc.timingGroup);
                    }

                    start = end;
                    end = new float3(
                        Conversion.GetWorldX(arc.endX),
                        Conversion.GetWorldY(arc.endY),
                        Conductor.Instance.GetFloorPositionFromTiming(arc.endTiming, arc.timingGroup)
                    );

                    CreateSegment(arcColorMaterialInstance, start, end, arc.timingGroup);
                    CreateJudgeEntities(arc, colorId, rawArcs.Count - 1, judges, rawArcs);
                    
                }

                colorId++;
            }

            JudgementSystem.Instance.rawArcs = new NativeArray<AffArc>(rawArcs.ToArray(), Allocator.Persistent);
            JudgementSystem.Instance.arcJudges = new NativeMatrIterator<ArcJudge>(utils.list_arr_2d(judges), Allocator.Persistent);
        }

        private unsafe void CreateSegment(Material arcColorMaterialInstance, float3 start, float3 end, int timingGroup)
        {
            Entity arcInstEntity = entityManager.Instantiate(arcNoteEntityPrefab);
            Entity arcShadowEntity = entityManager.Instantiate(arcShadowEntityPrefab);
            entityManager.SetSharedComponentData<RenderMesh>(arcInstEntity, new RenderMesh()
            {
                mesh = arcMesh,
                material = arcColorMaterialInstance
            });

            FloorPosition fpos = new FloorPosition()
            {
                value = start.z
            };

            entityManager.SetComponentData<FloorPosition>(arcInstEntity, fpos);
            entityManager.SetComponentData<FloorPosition>(arcShadowEntity, fpos);

            TimingGroup group = new TimingGroup()
            {
                value = timingGroup
            };

            entityManager.SetComponentData<TimingGroup>(arcInstEntity, group);
            entityManager.SetComponentData<TimingGroup>(arcShadowEntity, group);

            float dx = start.x - end.x;
            float dy = start.y - end.y;
            float dz = start.z - end.z;

            LocalToWorld ltwArc = new LocalToWorld()
            {
                Value = new float4x4(
                    1, 0, dx, start.x,
                    0, 1, dy, start.y,
                    0, 0, dz, 0,
                    0, 0, 0, 1
                )
            };

            LocalToWorld ltwShadow = ltwArc;
            ltwShadow.Value.c1.zw = new float2(1, 0);

            //Shear along xy + scale along z matrix
            entityManager.SetComponentData<LocalToWorld>(arcInstEntity, ltwArc);
            entityManager.SetComponentData<LocalToWorld>(arcShadowEntity, ltwShadow);

            //FIX THIS SHIT, WHAT IS HAPPENINGGGGGG
            entityManager.SetComponentData<ShaderRedmix>(arcInstEntity, new ShaderRedmix() { Value = 0f });

            int t1 = Conductor.Instance.GetFirstTimingFromFloorPosition(start.z + Constants.RenderFloorPositionRange, timingGroup);
            int t2 = Conductor.Instance.GetFirstTimingFromFloorPosition(end.z - Constants.RenderFloorPositionRange, timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;
            int disappearTime = (t1 < t2) ? t2 : t1;

            entityManager.SetComponentData<AppearTime>(arcInstEntity, new AppearTime()
            {
                value = appearTime
            });
            entityManager.SetComponentData<DisappearTime>(arcInstEntity, new DisappearTime()
            {
                value = disappearTime
            });
        }

        private void CreateHeightIndicator(AffArc arc, Material material)
        {
            Entity heightEntity = entityManager.Instantiate(heightIndicatorEntityPrefab);

            float height = Conversion.GetWorldY(arc.startY) - 0.45f;

            float x = Conversion.GetWorldX(arc.startX); 
            float y = height / 2;
            const float z = 0;

            const float scaleX = 2.34f;
            float scaleY = height;
            const float scaleZ = 1;

            Mesh mesh = entityManager.GetSharedComponentData<RenderMesh>(heightEntity).mesh; 
            entityManager.SetSharedComponentData<RenderMesh>(heightEntity, new RenderMesh()
            {
                mesh = mesh,
                material = material 
            });

            entityManager.SetComponentData<Translation>(heightEntity, new Translation()
            {
                Value = new float3(x, y, z)
            });
            entityManager.AddComponentData<NonUniformScale>(heightEntity, new NonUniformScale()
            {
                Value = new float3(scaleX, scaleY, scaleZ)
            });
            float floorpos = Conductor.Instance.GetFloorPositionFromTiming(arc.timing, arc.timingGroup);
            entityManager.AddComponentData<FloorPosition>(heightEntity, new FloorPosition()
            {
                value = floorpos
            });
            entityManager.SetComponentData<TimingGroup>(heightEntity, new TimingGroup()
            {
                value = arc.timingGroup
            });

            int t1 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, arc.timingGroup);
            int t2 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, arc.timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;

            entityManager.SetComponentData<AppearTime>(heightEntity, new AppearTime(){
                value = appearTime
            });
        }

        private void CreateHeadSegment(AffArc arc, Material material)
        {
            Entity headEntity = entityManager.Instantiate(headArcNoteEntityPrefab);
            entityManager.SetSharedComponentData<RenderMesh>(headEntity, new RenderMesh(){
                mesh = headMesh,
                material = material
            });

            float floorpos = Conductor.Instance.GetFloorPositionFromTiming(arc.timing, arc.timingGroup);
            entityManager.SetComponentData<FloorPosition>(headEntity, new FloorPosition()
            {
                value = floorpos 
            });

            float x = Conversion.GetWorldX(arc.startX); 
            float y = Conversion.GetWorldY(arc.startY); 
            const float z = 0;
            entityManager.SetComponentData<Translation>(headEntity, new Translation()
            {
                Value = new float3(x, y, z)
            });
            entityManager.SetComponentData<TimingGroup>(headEntity, new TimingGroup()
            {
                value = arc.timingGroup
            });

            int t1 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, arc.timingGroup);
            int t2 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, arc.timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;

            entityManager.SetComponentData<AppearTime>(headEntity, new AppearTime(){
                value = appearTime
            });
            
            //WHY WAS THIS HERE FUCKKKKKK
            entityManager.SetComponentData<ShaderCutoff>(headEntity, CutoffUtils.Unjudged);
        }

        private unsafe void CreateJudgeEntities(AffArc arc, int colorId, int cArcIdx, List<List<ArcJudge>> judges, List<AffArc> rawArcs)
        {
            float timeF = arc.timing;
            int timingEventIdx = Conductor.Instance.GetTimingEventIndexFromTiming(arc.timing, arc.timingGroup);
            TimingEvent timingEvent = Conductor.Instance.GetTimingEvent(timingEventIdx, arc.timingGroup);
            TimingEvent? nextEvent = Conductor.Instance.GetNextTimingEventOrNull(timingEventIdx, arc.timingGroup);

            while (timeF < arc.endTiming)
            {
                timeF += (timingEvent.bpm >= 255 ? 60_000f : 30_000f) / timingEvent.bpm;

                if (nextEvent is not null && nextEvent?.timing < timeF)
                {
                    timeF = (float)nextEvent?.timing;
                    timingEventIdx++;
                    timingEvent = Conductor.Instance.GetTimingEvent(timingEventIdx, arc.timingGroup);
                    nextEvent = Conductor.Instance.GetNextTimingEventOrNull(timingEventIdx, arc.timingGroup);
                }

                ArcJudge newJudge = new ArcJudge((int)timeF, cArcIdx, true);

                for (int i = 0; i < judges.Count; i++)
                {
                    ArcJudge judge = judges[colorId][i];

                    if (math.abs(judge.time - newJudge.time) < judgeStrictnessLeniency &&
                        math.distance(rawArcs[judge.rawArcIdx].PositionAt(judge.time), rawArcs[newJudge.rawArcIdx].PositionAt(newJudge.time)) < judgeStrictnessDist)
                    {
                        judges[colorId][i] = new ArcJudge(judge, false);
                        newJudge = new ArcJudge(newJudge, false);
                        break;
                    }
                }

                judges[colorId].Add(newJudge);
                ScoreManager.Instance.maxCombo++;
            }
        }
    }

}