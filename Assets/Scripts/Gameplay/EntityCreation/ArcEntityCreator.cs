using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using ArcCore.Gameplay.Utility;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Chunk;
using ArcCore.Parsing.Data;
using ArcCore.Gameplay.Data;
using ArcCore.Utilities.Extensions;
using ArcCore.Parsing;

namespace ArcCore.Gameplay.EntityCreation
{
    public class ArcEntityCreator
    {
        private Material arcMaterial;
        private Material heightMaterial;
        private Color redColor;
        private Mesh arcMesh;
        private Mesh headMesh;

        private Entity arcNoteEntityPrefab;
        private Entity headArcNoteEntityPrefab;
        private Entity heightIndicatorEntityPrefab;
        private Entity arcShadowEntityPrefab;
        private Entity arcJudgeEntityPrefab;

        private int colorShaderId;
        private int redColorShaderId;

        private EntityManager em;

        /// <summary>
        /// Time between two judge points of a similar area and differing colorIDs in which both points will be set as unscrict
        /// </summary>
        public const int judgeStrictnessLeniency = 100;
        /// <summary>
        /// The distance between two points (in world space) at which they will begin to be considered for unstrictness
        /// </summary>
        public const float judgeStrictnessDist = 1f;

        public ArcEntityCreator(
            World world,
            GameObject arcNotePrefab,
            GameObject headArcNotePrefab,
            GameObject heightIndicatorPrefab,
            GameObject arcShadowPrefab,
            GameObject arcJudgePrefab,
            Material arcMaterial,
            Material heightMaterial,
            Color redColor,
            Mesh arcMesh,
            Mesh headMesh)
        {
            em = world.EntityManager;
            var gocs = GameObjectConversionSettings.FromWorld(world, null);

            this.arcMaterial = arcMaterial;
            this.heightMaterial = heightMaterial;
            this.redColor = redColor;
            this.arcMesh = arcMesh;
            this.headMesh = headMesh;

            arcNoteEntityPrefab = gocs.ConvertToNote(arcNotePrefab, em);
            em.ExposeLocalToWorld(arcNoteEntityPrefab);

            headArcNoteEntityPrefab = gocs.ConvertToNote(headArcNotePrefab, em);

            heightIndicatorEntityPrefab = gocs.ConvertToNote(heightIndicatorPrefab, em);

            arcShadowEntityPrefab = gocs.ConvertToNote(arcShadowPrefab, em);
            em.ExposeLocalToWorld(arcShadowEntityPrefab);

            arcJudgeEntityPrefab = gocs.ConvertToEntity(arcJudgePrefab);

            colorShaderId = Shader.PropertyToID("_Color");
            redColorShaderId = Shader.PropertyToID("_RedCol");
        }

        public void CreateEntities(IChartParser parser)
        {
            Dictionary<int, Material> arcMaterials = new Dictionary<int, Material>();
            Dictionary<int, Material> heightIndicatorMaterials = new Dictionary<int, Material>();

            foreach (int i in parser.UsedArcColors)
            {
                Material arcColorMaterialInstance             = Object.Instantiate(arcMaterial);
                Material heightIndicatorColorMaterialInstance = Object.Instantiate(heightMaterial);

                arcColorMaterialInstance.SetColor(colorShaderId, GameSettings.Instance.GetArcColor(i));
                arcColorMaterialInstance.SetColor(redColorShaderId, redColor);

                heightIndicatorColorMaterialInstance.SetColor(colorShaderId, GameSettings.Instance.GetArcColor(i));

                arcMaterials.Add(i, arcColorMaterialInstance);
                heightIndicatorMaterials.Add(i, heightIndicatorColorMaterialInstance);
            }

            int colorId=0;

            var arcs = parser.Arcs;

            //SET UP NEW JUDGES HEREEEEE
            arcs.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });

            var connectedArcsIdEndpoint = new List<ArcEndpointData>();
            var startTimesById = new List<int>();

            foreach (ArcRaw arc in arcs)
            {
                int startGroupTime = default;

                //Precalc and assign a connected arc id to avoid having to figure out connection during gameplay
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
                        CreateHeadSegment(arc, arcMaterials[arc.color]);
                    }

                    if (isHeadArc || arc.startY != arc.endY)
                    {
                        CreateHeightIndicator(arc, heightIndicatorMaterials[arc.color]);
                    }
                }

                float startBpm = PlayManager.Conductor.GetTimingEventFromTiming(arc.timing, arc.timingGroup).bpm;

                //Generate arc segments and shadow segment(each segment is its own entity)
                int duration = arc.endTiming - arc.timing;

                if (duration == 0)
                {
                    float3 tstart = new float3(
                            Conversion.GetWorldX(arc.startX),
                            Conversion.GetWorldY(arc.startY),
                            PlayManager.Conductor.GetFloorPositionFromTiming(arc.timing, arc.timingGroup)
                        );
                    float3 tend = new float3(
                            Conversion.GetWorldX(arc.endX),
                            Conversion.GetWorldY(arc.endY),
                            PlayManager.Conductor.GetFloorPositionFromTiming(arc.endTiming, arc.timingGroup)
                        );
                    CreateSegment(arcMaterials[arc.color], tstart, tend, arc.timingGroup);
                    continue;
                }

                int v1 = duration < 1000 ? 14 : 7;
                float v2 = 1000f / (v1 * duration);
                float segmentLength = duration * v2;
                int segmentCount = (int)(duration / segmentLength) + 1;

                float3 start;
                float3 end = new float3(
                        Conversion.GetWorldX(arc.startX),
                        Conversion.GetWorldY(arc.startY),
                        PlayManager.Conductor.GetFloorPositionFromTiming(arc.timing, arc.timingGroup)
                    );

                for (int i = 0; i < segmentCount - 1; i++)
                {
                    float t = (i + 1) * segmentLength;
                    start = end;
                    end = new float3(
                        Conversion.GetWorldX(Conversion.GetXAt(t / duration, arc.startX, arc.endX, arc.easing)),
                        Conversion.GetWorldY(Conversion.GetYAt(t / duration, arc.startY, arc.endY, arc.easing)),
                        PlayManager.Conductor.GetFloorPositionFromTiming((int)(arc.timing + t), arc.timingGroup)
                    );

                    CreateSegment(arcMaterials[arc.color], start, end, arc.timingGroup);
                }

                start = end;
                end = new float3(
                    Conversion.GetWorldX(arc.endX),
                    Conversion.GetWorldY(arc.endY),
                    PlayManager.Conductor.GetFloorPositionFromTiming(arc.endTiming, arc.timingGroup)
                );

                CreateSegment(arcMaterials[arc.color], start, end, arc.timingGroup);
                CreateJudgeEntity(arc, colorId, startGroupTime, startBpm);

            }

            colorId++;
        }

        private void CreateSegment(Material arcColorMaterialInstance, float3 start, float3 end, int timingGroup)
        {
            Entity arcInstEntity = em.Instantiate(arcNoteEntityPrefab);
            Entity arcShadowEntity = em.Instantiate(arcShadowEntityPrefab);
            em.SetSharedComponentData<RenderMesh>(arcInstEntity, new RenderMesh()
            {
                mesh = arcMesh,
                material = arcColorMaterialInstance
            });

            em.SetComponentData(arcInstEntity, new FloorPosition(start.z));
            em.SetComponentData(arcShadowEntity, new FloorPosition(start.z));

            em.SetComponentData(arcInstEntity, new TimingGroup(timingGroup));
            em.SetComponentData(arcShadowEntity, new TimingGroup(timingGroup));

            em.SetComponentData(arcInstEntity, new EntityReference(arcShadowEntity));

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

            LocalToWorld ltwShadow = new LocalToWorld()
            {
                Value = new float4x4(
                    1, 0, dx, start.x,
                    0, 1, 0, 0,
                    0, 0, dz, 0,
                    0, 0, 0, 1
                )
            };

            //Shear along xy + scale along z matrix
            em.SetComponentData(arcInstEntity, ltwArc);
            em.SetComponentData(arcShadowEntity, ltwShadow);

            //FIX THIS SHIT, WHAT IS HAPPENINGGGGGG
            //entityManager.SetComponentData(arcInstEntity, new ShaderRedmix() { Value = 0f });

            int t1 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(start.z + Constants.RenderFloorPositionRange, timingGroup);
            int t2 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(end.z - Constants.RenderFloorPositionRange, timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;

            em.SetComponentData(arcInstEntity, new AppearTime(appearTime));
            em.SetComponentData(arcShadowEntity, new AppearTime(appearTime));
        }

        private void CreateHeightIndicator(ArcRaw arc, Material material)
        {
            Entity heightEntity = em.Instantiate(heightIndicatorEntityPrefab);

            float height = Conversion.GetWorldY(arc.startY) - 0.45f;

            float x = Conversion.GetWorldX(arc.startX); 
            float y = height / 2;
            const float z = 0;

            const float scaleX = 2.34f;
            float scaleY = height;
            const float scaleZ = 1;

            Mesh mesh = em.GetSharedComponentData<RenderMesh>(heightEntity).mesh; 
            em.SetSharedComponentData<RenderMesh>(heightEntity, new RenderMesh()
            {
                mesh = mesh,
                material = material 
            });

            em.SetComponentData(heightEntity, new Translation()
            {
                Value = new float3(x, y, z)
            });
            em.AddComponentData<NonUniformScale>(heightEntity, new NonUniformScale()
            {
                Value = new float3(scaleX, scaleY, scaleZ)
            });

            float floorpos = PlayManager.Conductor.GetFloorPositionFromTiming(arc.timing, arc.timingGroup);
            em.AddComponentData<FloorPosition>(heightEntity, new FloorPosition(floorpos));
            em.SetComponentData(heightEntity, new TimingGroup(arc.timingGroup));

            int t1 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, arc.timingGroup);
            int t2 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, arc.timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;

            em.SetComponentData(heightEntity, new AppearTime(appearTime));
            em.SetComponentData(heightEntity, new ChartTime(arc.timing));
        }

        private void CreateHeadSegment(ArcRaw arc, Material material)
        {
            Entity headEntity = em.Instantiate(headArcNoteEntityPrefab);

            em.SetSharedComponentData<RenderMesh>(headEntity, new RenderMesh(){
                mesh = headMesh,
                material = material
            });

            float floorpos = PlayManager.Conductor.GetFloorPositionFromTiming(arc.timing, arc.timingGroup);
            em.SetComponentData(headEntity, new FloorPosition(floorpos));

            float x = Conversion.GetWorldX(arc.startX); 
            float y = Conversion.GetWorldY(arc.startY); 
            const float z = 0;
            em.SetComponentData(headEntity, new Translation() { Value = math.float3(x, y, z) });

            em.SetComponentData(headEntity, new TimingGroup(arc.timingGroup));

            int t1 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, arc.timingGroup);
            int t2 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, arc.timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;

            em.SetComponentData(headEntity, new AppearTime(appearTime));
            em.SetComponentData(headEntity, new ChartTime(arc.timing));
        }

        private void CreateJudgeEntity(ArcRaw arc, int colorId, int startGroupTime, float startBpm)
        {

            Entity en = em.Instantiate(arcJudgeEntityPrefab);

            em.SetComponentData(en, new ChartTime(arc.timing));
            em.SetComponentData(en, ChartIncrTime.FromBpm(arc.timing, arc.endTiming, startBpm, out int comboCount));

            PlayManager.ScoreHandler.tracker.noteCount += comboCount;

            em.SetComponentData(en, new ColorID(colorId));
            em.SetComponentData(en,
                new ArcData(
                    math.float2(arc.startX, arc.startY),
                    math.float2(arc.endX, arc.endY),
                    arc.easing
                ));

            em.SetComponentData(en, new ArcGroupStartTime(startGroupTime));
            
        }

        /// <summary>
        /// Stores data requried to handle arc endpoints.
        /// </summary>
        private struct ArcEndpointData
        {
            public int timinggroup;
            public int time;
            public float xpos;
            public float ypos;

            public ArcEndpointData(int timinggroup, int time, float xpos, float ypos)
            {
                this.timinggroup = timinggroup;
                this.time = time;
                this.xpos = xpos;
                this.ypos = ypos;
            }

            public override bool Equals(object obj)
            {
                return obj is ArcEndpointData other &&
                       timinggroup == other.timinggroup &&
                       time == other.time &&
                       xpos == other.xpos &&
                       ypos == other.ypos;
            }

            public static bool operator ==(ArcEndpointData l, ArcEndpointData r) => l.Equals(r);
            public static bool operator !=(ArcEndpointData l, ArcEndpointData r) => !(l == r);

            public override int GetHashCode()
            {
                int hashCode = 1052165582;
                hashCode = hashCode * -1521134295 + timinggroup.GetHashCode();
                hashCode = hashCode * -1521134295 + time.GetHashCode();
                hashCode = hashCode * -1521134295 + xpos.GetHashCode();
                hashCode = hashCode * -1521134295 + ypos.GetHashCode();
                return hashCode;
            }

            public void Deconstruct(out int timinggroup, out int time, out float xpos, out float ypos)
            {
                timinggroup = this.timinggroup;
                time = this.time;
                xpos = this.xpos;
                ypos = this.ypos;
            }

            public static implicit operator (int timinggroup, int time, float xpos, float ypos)(ArcEndpointData value)
            {
                return (value.timinggroup, value.time, value.xpos, value.ypos);
            }

            public static implicit operator ArcEndpointData((int timinggroup, int time, float xpos, float ypos) value)
            {
                return new ArcEndpointData(value.timinggroup, value.time, value.xpos, value.ypos);
            }
        }
    }
}