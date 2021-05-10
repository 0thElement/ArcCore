using System.Collections.Generic;
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

        private EntityArchetype arcJudgeArchetype; //CONTINUE HERE DUMBFUCK

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

            arcJudgeArchetype = entityManager.CreateArchetype(
                
                //Chart time
                ComponentType.ReadOnly<ChartTime>(),
                //Judge time
                ComponentType.ReadWrite<ChartIncrTime>(),
                //Color
                ComponentType.ReadOnly<ColorID>(),
                //Arc data
                ComponentType.ReadOnly<ArcData>(),
                ComponentType.ReadOnly<ArcGroupStartTime>()
                
            );

            colorShaderId = Shader.PropertyToID("_Color");
            redColorShaderId = Shader.PropertyToID("_RedCol");

            JudgementSystem.Instance.SetupColors();
        }

        public void CreateEntities(List<List<AffArc>> affArcList)
        {
            int colorId=0;

            //SET UP NEW JUDGES HEREEEEE

            foreach (List<AffArc> listByColor in affArcList)
            {
                listByColor.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });

                Material arcColorMaterialInstance = Instantiate(arcMaterial);
                Material heightIndicatorColorMaterialInstance = Instantiate(heightMaterial);
                arcColorMaterialInstance.SetColor(colorShaderId, arcColors[colorId]);
                arcColorMaterialInstance.SetColor(redColorShaderId, redColor);
                heightIndicatorColorMaterialInstance.SetColor(colorShaderId, arcColors[colorId]);

                var connectedArcsIdEndpoint = new List<ArcEndpointData>();
                var startTimesById = new List<int>();

                foreach (AffArc arc in listByColor)
                {
                    int startGroupTime = default;

                    //Precalc and assign a connected arc id to avoid having to figure out connection during gameplay
                    //this is really dumb but i don't want to split this into another class
                    //placed into a new block to prevent data from being used later on
                    {
                        ArcEndpointData arcStartPoint = (arc.timingGroup, arc.timing, arc.startX, arc.startY);
                        ArcEndpointData arcEndPoint = (arc.timingGroup, arc.endTiming, arc.endX, arc.endY);

                        int arcId = connectedArcsIdEndpoint.Count;
                        startGroupTime = arc.timing;
                        bool isHeadArc = true;

                        for (int id = connectedArcsIdEndpoint.Count - 1; id >= 0; id--)
                        {
                            if (connectedArcsIdEndpoint[id] == arcStartPoint)
                            {
                                arcId = id;
                                startGroupTime = startTimesById[id];

                                isHeadArc = false;
                                connectedArcsIdEndpoint[id] = arcEndPoint;
                            }
                        }

                        if (isHeadArc)
                        {
                            connectedArcsIdEndpoint.Add(arcEndPoint);
                            startTimesById.Add(startGroupTime);
                            CreateHeadSegment(arc, arcColorMaterialInstance);
                        }

                        if (isHeadArc || arc.startY != arc.endY)
                        {
                            CreateHeightIndicator(arc, heightIndicatorColorMaterialInstance);
                        }
                    }

                    float startBpm = Conductor.Instance.GetTimingEventFromTiming(arc.timing, arc.timingGroup).bpm;

                    //Generate arc segments and shadow segment(each segment is its own entity)
                    int duration = arc.endTiming - arc.timing;
                    int v1 = duration < 1000 ? 14 : 7;
                    float v2 = 1000f / (v1 * duration);
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
                    CreateJudgeEntity(arc, colorId, startGroupTime, startBpm);

                }

                colorId++;
            }
        }

        private void CreateSegment(Material arcColorMaterialInstance, float3 start, float3 end, int timingGroup)
        {
            Entity arcInstEntity = entityManager.Instantiate(arcNoteEntityPrefab);
            Entity arcShadowEntity = entityManager.Instantiate(arcShadowEntityPrefab);
            entityManager.SetSharedComponentData<RenderMesh>(arcInstEntity, new RenderMesh()
            {
                mesh = arcMesh,
                material = arcColorMaterialInstance
            });

            entityManager.SetComponentData(arcInstEntity, new FloorPosition(start.z));
            entityManager.SetComponentData(arcShadowEntity, new FloorPosition(start.z));

            entityManager.SetComponentData(arcInstEntity, new TimingGroup(timingGroup));
            entityManager.SetComponentData(arcShadowEntity, new TimingGroup(timingGroup));

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
            ltwShadow.Value.c2.zw = math.float2(1, 0);

            //Shear along xy + scale along z matrix
            entityManager.SetComponentData(arcInstEntity, ltwArc);
            entityManager.SetComponentData(arcShadowEntity, ltwShadow);

            //FIX THIS SHIT, WHAT IS HAPPENINGGGGGG
            entityManager.SetComponentData(arcInstEntity, new ShaderRedmix() { Value = 0f });

            int t1 = Conductor.Instance.GetFirstTimingFromFloorPosition(start.z + Constants.RenderFloorPositionRange, timingGroup);
            int t2 = Conductor.Instance.GetFirstTimingFromFloorPosition(end.z - Constants.RenderFloorPositionRange, timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;
            int disappearTime = (t1 < t2) ? t2 : t1;

            entityManager.SetComponentData(arcInstEntity, new AppearTime(appearTime));
            entityManager.SetComponentData(arcInstEntity, new DisappearTime(disappearTime));
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

            entityManager.SetComponentData(heightEntity, new Translation()
            {
                Value = new float3(x, y, z)
            });
            entityManager.AddComponentData<NonUniformScale>(heightEntity, new NonUniformScale()
            {
                Value = new float3(scaleX, scaleY, scaleZ)
            });
            float floorpos = Conductor.Instance.GetFloorPositionFromTiming(arc.timing, arc.timingGroup);
            entityManager.AddComponentData<FloorPosition>(heightEntity, new FloorPosition(floorpos));
            entityManager.SetComponentData(heightEntity, new TimingGroup(arc.timingGroup));

            int t1 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, arc.timingGroup);
            int t2 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, arc.timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;

            entityManager.SetComponentData(heightEntity, new AppearTime(appearTime));
        }

        private void CreateHeadSegment(AffArc arc, Material material)
        {
            Entity headEntity = entityManager.Instantiate(headArcNoteEntityPrefab);
            entityManager.SetSharedComponentData<RenderMesh>(headEntity, new RenderMesh(){
                mesh = headMesh,
                material = material
            });

            float floorpos = Conductor.Instance.GetFloorPositionFromTiming(arc.timing, arc.timingGroup);
            entityManager.SetComponentData(headEntity, new FloorPosition(floorpos));

            float x = Conversion.GetWorldX(arc.startX); 
            float y = Conversion.GetWorldY(arc.startY); 
            const float z = 0;
            entityManager.SetComponentData(headEntity, new Translation()
            {
                Value = math.float3(x, y, z)
            });
            entityManager.SetComponentData(headEntity, new TimingGroup(arc.timingGroup));

            int t1 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, arc.timingGroup);
            int t2 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, arc.timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;

            entityManager.SetComponentData(headEntity, new AppearTime(appearTime));
            
            //Redo all the 
        }

        private void CreateJudgeEntity(AffArc arc, int colorId, int startGroupTime, float startBpm)
        {

            Entity en = entityManager.CreateEntity(arcJudgeArchetype);

            entityManager.SetComponentData(en, new ChartTime(arc.timing));
            entityManager.SetComponentData(en, ChartIncrTime.FromBpm(arc.timing, arc.endTiming, startBpm, out int comboCount));

            ScoreManager.Instance.maxCombo += comboCount;

            entityManager.SetComponentData(en, new ColorID(colorId));
            entityManager.SetComponentData(en,
                new ArcData(
                    math.float2(arc.startX, arc.startY),
                    math.float2(arc.endX, arc.endY),
                    arc.easing
                ));

            entityManager.SetComponentData(en, new ArcGroupStartTime(startGroupTime));
            
        }

        /// <summary>
        /// Stores data requried to handle arc endpoints.
        /// </summary>
        private struct ArcEndpointData
        {
            public int Item1;
            public int time;
            public float Item3;
            public float Item4;

            public ArcEndpointData(int item1, int time, float item3, float item4)
            {
                Item1 = item1;
                this.time = time;
                Item3 = item3;
                Item4 = item4;
            }

            public override bool Equals(object obj)
            {
                return obj is ArcEndpointData other &&
                       Item1 == other.Item1 &&
                       time == other.time &&
                       Item3 == other.Item3 &&
                       Item4 == other.Item4;
            }

            public static bool operator ==(ArcEndpointData l, ArcEndpointData r) => l.Equals(r);
            public static bool operator !=(ArcEndpointData l, ArcEndpointData r) => !(l == r);

            public override int GetHashCode()
            {
                int hashCode = 1052165582;
                hashCode = hashCode * -1521134295 + Item1.GetHashCode();
                hashCode = hashCode * -1521134295 + time.GetHashCode();
                hashCode = hashCode * -1521134295 + Item3.GetHashCode();
                hashCode = hashCode * -1521134295 + Item4.GetHashCode();
                return hashCode;
            }

            public void Deconstruct(out int item1, out int time, out float item3, out float item4)
            {
                item1 = Item1;
                time = this.time;
                item3 = Item3;
                item4 = Item4;
            }

            public static implicit operator (int, int time, float, float)(ArcEndpointData value)
            {
                return (value.Item1, value.time, value.Item3, value.Item4);
            }

            public static implicit operator ArcEndpointData((int, int time, float, float) value)
            {
                return new ArcEndpointData(value.Item1, value.time, value.Item3, value.Item4);
            }
        }
    }
}