using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using ArcCore.Utility;
using ArcCore.Data;

namespace ArcCore.MonoBehaviours.EntityCreation
{
    public class ArcEntityCreator : MonoBehaviour
    {
        public static ArcEntityCreator Instance { get; private set; }
        [SerializeField] private GameObject arcNotePrefab;
        [SerializeField] private GameObject headArcNotePrefab;
        [SerializeField] private GameObject heightIndicatorPrefab;
        [SerializeField] private Material arcMaterial;
        [SerializeField] private Material heightMaterial;
        [SerializeField] public Color[] arcColors;
        [SerializeField] private Mesh arcMesh;
        [SerializeField] private Mesh headMesh;
        private Entity arcNoteEntityPrefab;
        private Entity headArcNoteEntityPrefab;
        private Entity heightIndicatorEntityPrefab;
        private World defaultWorld;
        private EntityManager entityManager;
        private int colorShaderId;
        public EntityArchetype arcJudgeArchetype { get; private set; }
        public EntityArchetype arcFunnelArchetype { get; private set; }

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
            //Remove these component to allow direct access to localtoworld matrices
            //idk if this is a good way to set up an entity prefab in this case but this will do for now
            entityManager.RemoveComponent<Translation>(arcNoteEntityPrefab);
            entityManager.RemoveComponent<Rotation>(arcNoteEntityPrefab);

            headArcNoteEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(headArcNotePrefab, settings);

            heightIndicatorEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(heightIndicatorPrefab, settings);

            arcJudgeArchetype = entityManager.CreateArchetype(
                ComponentType.ReadOnly<ChartTime>(),
                ComponentType.ReadOnly<LinearPosGroup>(),
                ComponentType.ReadOnly<ColorID>(),
                ComponentType.ReadOnly<StrictArcJudge>(),
                ComponentType.ReadOnly<EntityReference>()
                );

            arcFunnelArchetype = entityManager.CreateArchetype(
                ComponentType.ReadWrite<ArcIsHit>(),
                ComponentType.ReadWrite<ArcIsRed>()
                );

            colorShaderId = Shader.PropertyToID("_Color");

            JudgementSystem.Instance.SetupColors();
        }

        public void CreateEntities(List<List<AffArc>> affArcList)
        {
            int colorId=0;
            List<Entity> createdJudgeEntities = new List<Entity>();
            foreach (List<AffArc> listByColor in affArcList)
            {
                listByColor.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });

                Material arcColorMaterialInstance = Instantiate(arcMaterial);
                Material heightIndicatorColorMaterialInstance = Instantiate(heightMaterial);
                arcColorMaterialInstance.SetColor(colorShaderId, arcColors[colorId]);
                heightIndicatorColorMaterialInstance.SetColor(colorShaderId, arcColors[colorId]);

                List<float4> connectedArcsIdEndpoint = new List<float4>();

                foreach (AffArc arc in listByColor)
                {
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

                    Entity arcDataEntity = entityManager.CreateEntity(arcFunnelArchetype);
                    entityManager.SetComponentData<ArcIsHit>(arcDataEntity, new ArcIsHit
                    {
                        Value = false
                    });
                    entityManager.SetComponentData<ArcIsRed>(arcDataEntity, new ArcIsRed
                    {
                        Value = false
                    });

                    //Generate arc segments and shadow segment(each segment is its own entity)
                    int duration = arc.endTiming - arc.timing;
                    int v1 = duration < 1000 ? 14 : 7;
                    float v2 = 1f / (v1 * duration / 1000f);
                    float segmentLength = duration * v2;
                    int segmentCount = (int)(segmentLength == 0 ? 0 : duration / segmentLength) + 1;

                    float3 start;
                    float3 end = new float3(
                        Convert.GetWorldX(arc.startX),
                        Convert.GetWorldY(arc.startY),
                        Conductor.Instance.GetFloorPositionFromTiming(arc.timing, arc.timingGroup)
                    );

                    for (int i = 0; i < segmentCount - 1; i++)
                    {
                        float t = (i + 1) * segmentLength;
                        start = end;
                        end = new float3(
                            Convert.GetWorldX(Convert.GetXAt(t / duration, arc.startX, arc.endX, arc.easing)),
                            Convert.GetWorldY(Convert.GetYAt(t / duration, arc.startY, arc.endY, arc.easing)),
                            Conductor.Instance.GetFloorPositionFromTiming((int)(arc.timing + t), arc.timingGroup)
                        );

                        CreateSegment(arcColorMaterialInstance, start, end, arc.timingGroup, arcDataEntity);
                    }

                    start = end;
                    end = new float3(
                        Convert.GetWorldX(arc.endX),
                        Convert.GetWorldY(arc.endY),
                        Conductor.Instance.GetFloorPositionFromTiming(arc.endTiming, arc.timingGroup)
                    );

                    CreateSegment(arcColorMaterialInstance, start, end, arc.timingGroup, arcDataEntity);
                    CreateJudgeEntities(arc, colorId, arcDataEntity, createdJudgeEntities);
                    
                }

                colorId++;
            }
        }

        private void CreateSegment(Material arcColorMaterialInstance, float3 start, float3 end, int timingGroup, Entity arcEntity)
        {
            Entity arcInstEntity = entityManager.Instantiate(arcNoteEntityPrefab);
            entityManager.SetSharedComponentData<RenderMesh>(arcInstEntity, new RenderMesh()
            {
                mesh = arcMesh,
                material = arcColorMaterialInstance
            });
            entityManager.SetComponentData<FloorPosition>(arcInstEntity, new FloorPosition()
            {
                Value = start.z
            });

            float dx = start.x - end.x;
            float dy = start.y - end.y;
            float dz = start.z - end.z;

            //Shear along xy + scale along z matrix
            entityManager.SetComponentData<LocalToWorld>(arcInstEntity, new LocalToWorld()
            {
                Value = new float4x4(
                    1, 0, dx, start.x,
                    0, 1, dy, start.y,
                    0, 0, dz, 0,
                    0, 0, 0,  1
                )
            });

            entityManager.SetComponentData<TimingGroup>(arcInstEntity, new TimingGroup()
            {
                Value = timingGroup
            });

            entityManager.SetComponentData<EntityReference>(arcInstEntity, new EntityReference()
            {
                Value = arcEntity
            });

            entityManager.SetComponentData<ShouldCutOff>(arcInstEntity, new ShouldCutOff()
            {
                Value = 1f
            });
        }

        private void CreateHeightIndicator(AffArc arc, Material material)
        {
            Entity heightEntity = entityManager.Instantiate(heightIndicatorEntityPrefab);

            float height = Convert.GetWorldY(arc.startY) - 0.45f;

            float x = Convert.GetWorldX(arc.startX); 
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
            entityManager.AddComponentData<FloorPosition>(heightEntity, new FloorPosition()
            {
                Value = Conductor.Instance.GetFloorPositionFromTiming(arc.timing, arc.timingGroup)
            });
            entityManager.SetComponentData<TimingGroup>(heightEntity, new TimingGroup()
            {
                Value = arc.timingGroup
            });
        }

        private void CreateHeadSegment(AffArc arc, Material material)
        {
            Entity headEntity = entityManager.Instantiate(headArcNoteEntityPrefab);
            entityManager.SetSharedComponentData<RenderMesh>(headEntity, new RenderMesh(){
                mesh = headMesh,
                material = material
            });
            entityManager.SetComponentData<FloorPosition>(headEntity, new FloorPosition()
            {
                Value = Conductor.Instance.GetFloorPositionFromTiming(arc.timing, arc.timingGroup)
            });

            float x = Convert.GetWorldX(arc.startX); 
            float y = Convert.GetWorldY(arc.startY); 
            const float z = 0;
            entityManager.SetComponentData<Translation>(headEntity, new Translation()
            {
                Value = new float3(x, y, z)
            });
            entityManager.SetComponentData<TimingGroup>(headEntity, new TimingGroup()
            {
                Value = arc.timingGroup
            });
            entityManager.SetComponentData<ShouldCutOff>(headEntity, new ShouldCutOff()
            {
                Value = 1f
            });
        }

        private void CreateJudgeEntities(AffArc arc, int colorId, Entity arcEntity, List<Entity> createdJudgeEntities)
        {
            float timeF = arc.timing;
            int timingEventIdx = Conductor.Instance.GetTimingEventIndexFromTiming(arc.timing, arc.timingGroup);
            TimingEvent timingEvent = Conductor.Instance.GetTimingEvent(timingEventIdx, arc.timingGroup);
            TimingEvent? nextEvent = Conductor.Instance.GetNextTimingEventOrNull(timingEventIdx, arc.timingGroup);

            while (timeF < arc.endTiming)
            {
                timeF += (timingEvent.bpm >= 255 ? 60_000f : 30_000f) / timingEvent.bpm;

                if (nextEvent.HasValue && nextEvent.Value.timing < timeF)
                {
                    timeF = nextEvent.Value.timing;
                    timingEventIdx++;
                    timingEvent = Conductor.Instance.GetTimingEvent(timingEventIdx, arc.timingGroup);
                    nextEvent = Conductor.Instance.GetNextTimingEventOrNull(timingEventIdx, arc.timingGroup);
                }

                int time = (int)timeF;

                float timePosEnd = math.min(timeF + Constants.FarWindow, arc.endTiming);

                float arcStartX = Convert.GetWorldX(arc.startX);
                float arcStartY = Convert.GetWorldY(arc.startY);
                float arcEndX = Convert.GetWorldX(arc.endX);
                float arcEndY = Convert.GetWorldY(arc.endY);

                LinearPosGroup currentLpg = new LinearPosGroup()
                {
                    startPosition = new float2(
                        Convert.GetXAt(Convert.RatioBetween(arc.timing, arc.endTiming, timeF), arcStartX, arcEndX, arc.easing),
                        Convert.GetYAt(Convert.RatioBetween(arc.timing, arc.endTiming, timeF), arcStartY, arcEndY, arc.easing)
                    ),
                    startTime = (int)timeF,
                    endPosition = new float2(
                        Convert.GetXAt(Convert.RatioBetween(arc.timing, arc.endTiming, timePosEnd), arcStartX, arcEndX, arc.easing),
                        Convert.GetYAt(Convert.RatioBetween(arc.timing, arc.endTiming, timePosEnd), arcStartY, arcEndY, arc.easing)
                    ),
                    endTime = (int)timePosEnd
                };

                bool IsStrict = true;
                foreach (Entity en in createdJudgeEntities)
                {
                    if (entityManager.GetComponentData<ColorID>(en).Value != colorId)
                    {
                        LinearPosGroup otherLpg = entityManager.GetComponentData<LinearPosGroup>(en);
                        if(math.abs(otherLpg.TimeCenter() - currentLpg.TimeCenter()) < judgeStrictnessLeniency &&
                           math.distance(otherLpg.PosAt(time), currentLpg.PosAt(time)) < judgeStrictnessDist)
                        {
                            entityManager.SetComponentData<StrictArcJudge>(en, new StrictArcJudge()
                            {
                                Value = false
                            });

                            IsStrict = false;
                            break;
                        }
                    }
                }

                Entity judgeEntity = entityManager.CreateEntity(arcJudgeArchetype);
                entityManager.SetComponentData<ChartTime>(judgeEntity, new ChartTime()
                {
                    Value = time
                });
                entityManager.SetComponentData<LinearPosGroup>(judgeEntity, currentLpg);
                entityManager.SetComponentData<ColorID>(judgeEntity, new ColorID()
                {
                    Value = colorId
                });
                entityManager.SetComponentData<EntityReference>(judgeEntity, new EntityReference()
                {
                    Value = arcEntity
                });
                entityManager.SetComponentData<StrictArcJudge>(judgeEntity, new StrictArcJudge()
                {
                    Value = IsStrict
                });

                createdJudgeEntities.Add(judgeEntity);

                ScoreManager.Instance.maxCombo++;
            }
        }
    }
}