using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using ArcCore.Gameplay.Components;
using ArcCore.Parsing.Aff;
using ArcCore.Utilities.Extensions;
using ArcCore.Gameplay.Systems.Judgement;
using Unity.Collections;

namespace ArcCore.Gameplay.Behaviours.EntityCreation
{
    public class ArcEntityCreator : ECSMonoBehaviour
    {
        public static ArcEntityCreator Instance { get; private set; }
        [SerializeField] private GameObject arcNotePrefab;
        [SerializeField] private GameObject headArcNotePrefab;
        [SerializeField] private GameObject heightIndicatorPrefab;
        [SerializeField] private GameObject arcShadowPrefab;
        [SerializeField] public Color[] arcColors;
        [SerializeField] private Color redColor;
        private Entity arcNoteEntityPrefab;
        private Entity headArcNoteEntityPrefab;
        private Entity heightIndicatorEntityPrefab;
        private Entity arcShadowEntityPrefab;
        private int colorShaderId;
        private int highlightShaderId;
        private int redColorShaderId;

        private EntityArchetype arcJudgeArchetype; //CONTINUE HERE DUMBFUCK
        [HideInInspector] private Material arcMaterial;
        [HideInInspector] private Material heightMaterial;
        [HideInInspector] private Mesh arcMesh;
        [HideInInspector] private Mesh headMesh;

        [HideInInspector] public static int ColorCount = 2;
        [HideInInspector] public static int GroupCount = 0;
        [HideInInspector] private List<RenderMesh> initialRenderMeshes;
        [HideInInspector] private List<RenderMesh> highlightRenderMeshes;
        [HideInInspector] private List<RenderMesh> grayoutRenderMeshes;
        [HideInInspector] private List<RenderMesh> headRenderMeshes;
        [HideInInspector] public RenderMesh ArcShadowRenderMesh;
        [HideInInspector] public RenderMesh ArcShadowGrayoutRenderMesh;

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

            arcNoteEntityPrefab = GameObjectConversionSettings.ConvertToNote(arcNotePrefab, EntityManager);
            EntityManager.ExposeLocalToWorld(arcNoteEntityPrefab);

            headArcNoteEntityPrefab = GameObjectConversionSettings.ConvertToNote(headArcNotePrefab, EntityManager);

            heightIndicatorEntityPrefab = GameObjectConversionSettings.ConvertToNote(heightIndicatorPrefab, EntityManager);

            arcShadowEntityPrefab = GameObjectConversionSettings.ConvertToNote(arcShadowPrefab, EntityManager);
            EntityManager.ExposeLocalToWorld(arcShadowEntityPrefab);
            
            arcJudgeArchetype = EntityManager.CreateArchetype(
                
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
            
            colorShaderId = Shader.PropertyToID("_Color");
            highlightShaderId = Shader.PropertyToID("_Highlight");

            arcMaterial = arcNotePrefab.GetComponent<Renderer>().sharedMaterial;
            heightMaterial = heightIndicatorPrefab.GetComponent<Renderer>().sharedMaterial;
            arcMesh = arcNotePrefab.GetComponent<MeshFilter>().sharedMesh;
            headMesh = headArcNotePrefab.GetComponent<MeshFilter>().sharedMesh;

            RenderMesh shadowRenderMesh = EntityManager.GetSharedComponentData<RenderMesh>(arcShadowEntityPrefab);
            Material shadowMaterial = shadowRenderMesh.material;
            Material shadowGrayoutMaterial = Instantiate(shadowMaterial);

            shadowGrayoutMaterial.SetFloat(highlightShaderId, -1);

            ArcShadowRenderMesh = new RenderMesh()
            {
                mesh = shadowRenderMesh.mesh,
                material = shadowMaterial
            };
            ArcShadowGrayoutRenderMesh = new RenderMesh()
            {
                mesh = shadowRenderMesh.mesh,
                material = shadowGrayoutMaterial
            };
        }

        public void CreateEntities(List<List<AffArc>> affArcList)
        {
            int colorId=0;
            var connectedArcsIdEndpoint = new List<ArcEndpointData>();
            ClearRenderMeshList();

            //SET UP NEW JUDGES HEREEEEE

            foreach (List<AffArc> listByColor in affArcList)
            {
                listByColor.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });

                Material arcColorMaterialInstance = Instantiate(arcMaterial);
                Material heightIndicatorColorMaterialInstance = Instantiate(heightMaterial);
                arcColorMaterialInstance.SetColor(colorShaderId, arcColors[colorId]);
                arcColorMaterialInstance.SetColor(redColorShaderId, redColor);
                heightIndicatorColorMaterialInstance.SetColor(colorShaderId, arcColors[colorId]);

                RenderMesh renderMesh = new RenderMesh()
                {
                    mesh = arcMesh,
                    material = arcColorMaterialInstance
                };
                RegisterRenderMeshVariants(renderMesh);

                foreach (AffArc arc in listByColor)
                {
                    int startGroupTime = default;

                    //Precalc and assign a connected arc id to avoid having to figure out connection during gameplay
                    //placed into a new block to prevent data from being used later on
                    ArcEndpointData arcStartPoint = (arc.timingGroup, arc.timing, arc.startX, arc.startY, colorId);
                    ArcEndpointData arcEndPoint = (arc.timingGroup, arc.endTiming, arc.endX, arc.endY, colorId);

                    int arcId = connectedArcsIdEndpoint.Count;
                    startGroupTime = arc.timing;
                    bool isHeadArc = true;

                    for (int id = connectedArcsIdEndpoint.Count - 1; id >= 0; id--)
                    {
                        if (connectedArcsIdEndpoint[id] == arcStartPoint)
                        {
                            arcId = id;

                            isHeadArc = false;
                            connectedArcsIdEndpoint[id] = arcEndPoint;
                        }
                    }

                    if (isHeadArc)
                    {
                        connectedArcsIdEndpoint.Add(arcEndPoint);
                        CreateHeadSegment(arc, arcColorMaterialInstance);
                    }

                    if (isHeadArc || arc.startY != arc.endY)
                    {
                        CreateHeightIndicator(arc, heightIndicatorColorMaterialInstance);
                    }

                    float startBpm = Conductor.Instance.GetTimingEventFromTiming(arc.timing, arc.timingGroup).bpm;

                    //Generate arc segments and shadow segment(each segment is its own entity)
                    int duration = arc.endTiming - arc.timing;

                    if (duration == 0)
                    {
                        float3 tstart = new float3(
                            Conversion.GetWorldX(arc.startX),
                            Conversion.GetWorldY(arc.startY),
                            Conductor.Instance.GetFloorPositionFromTiming(arc.timing, arc.timingGroup)
                        );
                        float3 tend = new float3(
                            Conversion.GetWorldX(arc.endX),
                            Conversion.GetWorldY(arc.endY),
                            Conductor.Instance.GetFloorPositionFromTiming(arc.endTiming, arc.timingGroup)
                        );
                        CreateSegment(renderMesh, tstart, tend, arc.timingGroup, arc.timing, arc.endTiming, arcId);
                        continue;
                    }

                    int v1 = duration < 1000 ? 14 : 7;
                    float v2 = 1000f / (v1 * duration);
                    float segmentLength = duration * v2;
                    int segmentCount = (int)(duration / segmentLength) + 1;

                    int fromTiming;
                    int toTiming = arc.timing;

                    float3 start;
                    float3 end = new float3(
                        Conversion.GetWorldX(arc.startX),
                        Conversion.GetWorldY(arc.startY),
                        Conductor.Instance.GetFloorPositionFromTiming(arc.timing, arc.timingGroup)
                    );

                    for (int i = 0; i < segmentCount - 1; i++)
                    {
                        int t = (int)((i + 1) * segmentLength);

                        fromTiming = toTiming;
                        toTiming = arc.timing + t;

                        start = end;
                        end = new float3(
                            Conversion.GetWorldX(Conversion.GetXAt(t / duration, arc.startX, arc.endX, arc.easing)),
                            Conversion.GetWorldY(Conversion.GetYAt(t / duration, arc.startY, arc.endY, arc.easing)),
                            Conductor.Instance.GetFloorPositionFromTiming(toTiming, arc.timingGroup)
                        );

                        CreateSegment(renderMesh, start, end, arc.timingGroup, fromTiming, toTiming, arcId);
                    }

                    fromTiming = toTiming;
                    toTiming = arc.endTiming;
                    
                    start = end;
                    end = new float3(
                        Conversion.GetWorldX(arc.endX),
                        Conversion.GetWorldY(arc.endY),
                        Conductor.Instance.GetFloorPositionFromTiming(arc.endTiming, arc.timingGroup)
                    );

                    CreateSegment(renderMesh, start, end, arc.timingGroup, fromTiming, toTiming, arcId);
                    CreateJudgeEntity(arc, colorId, arcId, startGroupTime, startBpm);

                }

                colorId++;
            }
            ColorCount = colorId;
            GroupCount = connectedArcsIdEndpoint.Count;

            Debug.Log(GroupCount);

            //TEMPORARY
            if (ArcCollisionCheckSystem.arcGroupHeldState.IsCreated) ArcCollisionCheckSystem.arcGroupHeldState.Dispose();
            ArcCollisionCheckSystem.arcGroupHeldState = new NativeArray<int>(GroupCount, Allocator.Persistent);
            if (ArcCollisionCheckSystem.arcColorTouchDataArray.IsCreated) ArcCollisionCheckSystem.arcColorTouchDataArray.Dispose();
            ArcCollisionCheckSystem.arcColorTouchDataArray = new NativeArray<ArcColorTouchData>(ColorCount, Allocator.Persistent);
        }

        private void CreateSegment(RenderMesh renderMesh, float3 start, float3 end, int timingGroup, int timing, int endTiming, int groupId)
        {
            Entity arcInstEntity = EntityManager.Instantiate(arcNoteEntityPrefab);
            EntityManager.SetSharedComponentData<RenderMesh>(arcInstEntity, renderMesh); 

            EntityManager.SetComponentData(arcInstEntity, new FloorPosition(start.z));

            EntityManager.SetComponentData(arcInstEntity, new TimingGroup(timingGroup));

            float dx = start.x - end.x;
            float dy = start.y - end.y;
            float dz = start.z - end.z;

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
            EntityManager.SetComponentData(arcInstEntity, ltwArc);

            EntityManager.SetComponentData(arcInstEntity, new BaseOffset(new float4(start.x, start.y, 0, 0)));
            EntityManager.SetComponentData(arcInstEntity, new BaseShear(new float4(dx, dy, dz, 0)));

            EntityManager.SetComponentData(arcInstEntity, new Cutoff(false));


            int t1 = Conductor.Instance.GetFirstTimingFromFloorPosition(start.z + Constants.RenderFloorPositionRange, timingGroup);
            int t2 = Conductor.Instance.GetFirstTimingFromFloorPosition(end.z - Constants.RenderFloorPositionRange, timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;

            EntityManager.SetComponentData(arcInstEntity, new AppearTime(appearTime));
            EntityManager.SetComponentData(arcInstEntity, new DestroyOnTiming(endTiming + Constants.HoldLostWindow));
            EntityManager.SetComponentData(arcInstEntity, new ArcGroupID(groupId));
            EntityManager.SetComponentData(arcInstEntity, new ChartTime(timing));

            if (timing < endTiming)
            {
                Entity arcShadowEntity = EntityManager.Instantiate(arcShadowEntityPrefab);
                EntityManager.SetComponentData(arcShadowEntity, new FloorPosition(start.z));
                EntityManager.SetComponentData(arcShadowEntity, new TimingGroup(timingGroup));
                EntityManager.SetSharedComponentData<RenderMesh>(arcShadowEntity, ArcShadowRenderMesh);
                LocalToWorld ltwShadow = new LocalToWorld()
                {
                    Value = new float4x4(
                        1, 0, dx, start.x,
                        0, 1, 0, 0,
                        0, 0, dz, 0,
                        0, 0, 0, 1
                    )
                };
                EntityManager.SetComponentData(arcShadowEntity, new BaseOffset(new float4(start.x, 0, 0, 0)));
                EntityManager.SetComponentData(arcShadowEntity, new BaseShear(new float4(dx, 0, dz, 0)));
                EntityManager.SetComponentData(arcShadowEntity, new Cutoff(false));
                EntityManager.SetComponentData(arcShadowEntity, ltwShadow);
                EntityManager.SetComponentData(arcShadowEntity, new AppearTime(appearTime));
                EntityManager.SetComponentData(arcShadowEntity, new DestroyOnTiming(endTiming + Constants.HoldLostWindow));
                EntityManager.SetComponentData(arcShadowEntity, new ArcGroupID(groupId));
                EntityManager.SetComponentData(arcShadowEntity, new ChartTime(timing));
            }
        }

        private void CreateHeightIndicator(AffArc arc, Material material)
        {
            Entity heightEntity = EntityManager.Instantiate(heightIndicatorEntityPrefab);

            float height = Conversion.GetWorldY(arc.startY) - 0.45f;

            float x = Conversion.GetWorldX(arc.startX); 
            float y = height / 2;
            const float z = 0;

            const float scaleX = 2.34f;
            float scaleY = height;
            const float scaleZ = 1;

            Mesh mesh = EntityManager.GetSharedComponentData<RenderMesh>(heightEntity).mesh; 
            EntityManager.SetSharedComponentData(heightEntity, new RenderMesh()
            {
                mesh = mesh,
                material = material 
            });

            EntityManager.SetComponentData(heightEntity, new Translation()
            {
                Value = new float3(x, y, z)
            });
            EntityManager.AddComponentData(heightEntity, new NonUniformScale()
            {
                Value = new float3(scaleX, scaleY, scaleZ)
            });
            float floorpos = Conductor.Instance.GetFloorPositionFromTiming(arc.timing, arc.timingGroup);
            EntityManager.AddComponentData(heightEntity, new FloorPosition(floorpos));
            EntityManager.SetComponentData(heightEntity, new TimingGroup(arc.timingGroup));

            int t1 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, arc.timingGroup);
            int t2 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, arc.timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;

            EntityManager.SetComponentData(heightEntity, new AppearTime(appearTime));
            EntityManager.SetComponentData(heightEntity, new DestroyOnTiming(arc.timing));
        }

        private void CreateHeadSegment(AffArc arc, Material material)
        {
            Entity headEntity = EntityManager.Instantiate(headArcNoteEntityPrefab);

            EntityManager.SetSharedComponentData(headEntity, new RenderMesh(){
                mesh = headMesh,
                material = material
            });

            float floorpos = Conductor.Instance.GetFloorPositionFromTiming(arc.timing, arc.timingGroup);
            EntityManager.SetComponentData(headEntity, new FloorPosition(floorpos));

            float x = Conversion.GetWorldX(arc.startX); 
            float y = Conversion.GetWorldY(arc.startY); 
            const float z = 0;
            EntityManager.SetComponentData(headEntity, new Translation() { Value = math.float3(x, y, z) });

            EntityManager.SetComponentData(headEntity, new TimingGroup(arc.timingGroup));

            int t1 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, arc.timingGroup);
            int t2 = Conductor.Instance.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, arc.timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;

            EntityManager.SetComponentData(headEntity, new AppearTime(appearTime));
            EntityManager.SetComponentData(headEntity, new DestroyOnTiming(arc.timing));
        }

        private void CreateJudgeEntity(AffArc arc, int colorId, int groupId, int startGroupTime, float startBpm)
        {

            Entity en = EntityManager.CreateEntity(arcJudgeArchetype);

            EntityManager.SetComponentData(en, new ChartTime(arc.timing));
            EntityManager.SetComponentData(en, ChartIncrTime.FromBpm(arc.timing, arc.endTiming, startBpm, out int comboCount));

            ScoreManager.Instance.tracker.noteCount += comboCount;

            EntityManager.SetSharedComponentData(en, new ArcColorID(colorId));
            EntityManager.SetComponentData(en,
                new ArcData(
                    Conversion.GetWorldPos(math.float2(arc.startX, arc.startY)),
                    Conversion.GetWorldPos(math.float2(arc.endX, arc.endY)),
                    arc.timing,
                    arc.endTiming,
                    arc.easing
                ));

            EntityManager.SetComponentData(en, new ArcGroupID(groupId));
            EntityManager.SetComponentData(en, new DestroyOnTiming(arc.endTiming + Constants.HoldLostWindow));
            
        }

        private void ClearRenderMeshList()
        {
            initialRenderMeshes = new List<RenderMesh>();
            highlightRenderMeshes = new List<RenderMesh>();
            grayoutRenderMeshes = new List<RenderMesh>();
            headRenderMeshes = new List<RenderMesh>();
        }
        private void RegisterRenderMeshVariants(RenderMesh initial)
        {
            initialRenderMeshes.Add(initial);
            
            Material highlightMat = Instantiate(initial.material);
            highlightMat.SetFloat(highlightShaderId, 1);
            Material grayoutMat = Instantiate(initial.material);
            grayoutMat.SetFloat(highlightShaderId,-1);

            highlightRenderMeshes.Add(new RenderMesh{
                mesh = initial.mesh,
                material = highlightMat
            });
            grayoutRenderMeshes.Add(new RenderMesh{
                mesh = initial.mesh,
                material = grayoutMat
            });
            headRenderMeshes.Add(new RenderMesh{
                mesh = headMesh,
                material = initial.material
            });
        }
        public (RenderMesh, RenderMesh, RenderMesh, RenderMesh) GetRenderMeshVariants(int color)
        {
            return (initialRenderMeshes[color], highlightRenderMeshes[color], grayoutRenderMeshes[color], headRenderMeshes[color]);
        }
        public void UpdateRenderMeshVariants(int color, RenderMesh newinitial, RenderMesh newhighlight, RenderMesh newgrayout)
        {
            initialRenderMeshes[color] = newinitial;
            highlightRenderMeshes[color] = newhighlight;
            grayoutRenderMeshes[color] = newgrayout;
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
            public int Item5;

            public ArcEndpointData(int item1, int time, float item3, float item4, int item5)
            {
                Item1 = item1;
                this.time = time;
                Item3 = item3;
                Item4 = item4;
                Item5 = item5; 
            }

            public override bool Equals(object obj)
            {
                return obj is ArcEndpointData other &&
                       Item1 == other.Item1 &&
                       time == other.time &&
                       Item3 == other.Item3 &&
                       Item4 == other.Item4 &&
                       Item5 == other.Item5;
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
                hashCode = hashCode * -1521134295 + Item5.GetHashCode();
                return hashCode;
            }

            public void Deconstruct(out int item1, out int time, out float item3, out float item4, out float item5)
            {
                item1 = Item1;
                time = this.time;
                item3 = Item3;
                item4 = Item4;
                item5 = Item5;
            }

            public static implicit operator (int, int time, float, float, int)(ArcEndpointData value)
            {
                return (value.Item1, value.time, value.Item3, value.Item4, value.Item5);
            }

            public static implicit operator ArcEndpointData((int, int time, float, float, int) value)
            {
                return new ArcEndpointData(value.Item1, value.time, value.Item3, value.Item4, value.Item5);
            }
        }
    }
}