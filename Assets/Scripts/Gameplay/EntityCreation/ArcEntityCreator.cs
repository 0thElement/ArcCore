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
using ArcCore.Gameplay.Systems;

namespace ArcCore.Gameplay.EntityCreation
{
    public class ArcEntityCreator
    {
        private Material arcMaterial;
        private Material heightMaterial;
        private Color redColor;
        private Mesh arcMesh;
        private Mesh headMesh;
        private Mesh heightMesh;
        private RenderMesh shadowRenderMesh;
        private RenderMesh shadowGrayoutRenderMesh;

        private Entity arcNoteEntityPrefab;
        private Entity headArcNoteEntityPrefab;
        private Entity heightIndicatorEntityPrefab;
        private Entity arcShadowEntityPrefab;
        private Entity arcJudgeEntityPrefab;

        private GameObject arcApproachIndicatorPrefab;
        private GameObject arcParticlePrefab;

        private int colorShaderId;
        private int redColorShaderId;
        private int highlightShaderId;

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
            GameObject arcApproachIndicatorPrefab,
            GameObject arcParticlePrefab,
            Color redColor)
        {
            em = world.EntityManager;
            var gocs = GameObjectConversionSettings.FromWorld(world, null);

            this.redColor = redColor;
            this.arcApproachIndicatorPrefab = arcApproachIndicatorPrefab;
            this.arcParticlePrefab = arcParticlePrefab;

            //Entity prefabs conversion
            arcNoteEntityPrefab = gocs.ConvertToNote(arcNotePrefab, em);
            em.ExposeLocalToWorld(arcNoteEntityPrefab);

            headArcNoteEntityPrefab = gocs.ConvertToNote(headArcNotePrefab, em);

            heightIndicatorEntityPrefab = gocs.ConvertToNote(heightIndicatorPrefab, em);

            arcShadowEntityPrefab = gocs.ConvertToNote(arcShadowPrefab, em);
            em.ExposeLocalToWorld(arcShadowEntityPrefab);

            arcJudgeEntityPrefab = gocs.ConvertToEntity(arcJudgePrefab);

            //Shader ID
            colorShaderId = Shader.PropertyToID("_Color");
            redColorShaderId = Shader.PropertyToID("_RedCol");
            highlightShaderId = Shader.PropertyToID("_Highlight");

            //Extract material and mesh from prefab object
            arcMaterial = arcNotePrefab.GetComponent<Renderer>().sharedMaterial;
            heightMaterial = heightIndicatorPrefab.GetComponent<Renderer>().sharedMaterial;
            arcMesh = arcNotePrefab.GetComponent<MeshFilter>().sharedMesh;
            headMesh = headArcNotePrefab.GetComponent<MeshFilter>().sharedMesh;
            heightMesh = heightIndicatorPrefab.GetComponent<MeshFilter>().sharedMesh;

            RenderMesh shadowRenderMesh = em.GetSharedComponentData<RenderMesh>(arcShadowEntityPrefab);
            Material shadowMaterial = shadowRenderMesh.material;
            Material shadowGrayoutMaterial = Object.Instantiate(shadowMaterial);

            shadowGrayoutMaterial.SetFloat(highlightShaderId, -1);

            shadowRenderMesh = new RenderMesh()
            {
                mesh = shadowRenderMesh.mesh,
                material = shadowMaterial
            };
            shadowGrayoutRenderMesh = new RenderMesh()
            {
                mesh = shadowRenderMesh.mesh,
                material = shadowGrayoutMaterial
            };
        }

        public void CreateEntitiesAndGetMeshes(
            IChartParser parser,
            out Dictionary<int, RenderMesh> initial,
            out Dictionary<int, RenderMesh> highlight,
            out Dictionary<int, RenderMesh> grayout,
            out Dictionary<int, RenderMesh> head,
            out Dictionary<int, RenderMesh> height,
            out RenderMesh shadow,
            out RenderMesh shadowGrayout)
        {
            //SETUP MATERIALS
            initial = new Dictionary<int, RenderMesh>();
            highlight = new Dictionary<int, RenderMesh>();
            grayout = new Dictionary<int, RenderMesh>();
            head = new Dictionary<int, RenderMesh>();
            height = new Dictionary<int, RenderMesh>();

            for (int i = 0; i < parser.MaxArcColor; i++)
            {
                Material arcColorMaterialInstance             = Object.Instantiate(arcMaterial);
                Material heightIndicatorColorMaterialInstance = Object.Instantiate(heightMaterial);

                arcColorMaterialInstance.SetColor(colorShaderId, GameSettings.Instance.GetArcColor(i));
                heightIndicatorColorMaterialInstance.SetColor(colorShaderId, GameSettings.Instance.GetArcColor(i));

                Material highlightMat = Object.Instantiate(arcColorMaterialInstance);
                highlightMat.SetFloat(highlightShaderId, 1);
                Material grayoutMat = Object.Instantiate(arcColorMaterialInstance);
                grayoutMat.SetFloat(highlightShaderId,-1);

                initial.Add(i, new RenderMesh {
                    mesh = arcMesh,
                    material = arcColorMaterialInstance
                });
                highlight.Add(i, new RenderMesh {
                    mesh = arcMesh,
                    material = highlightMat
                });
                grayout.Add(i, new RenderMesh {
                    mesh = arcMesh,
                    material = grayoutMat
                });
                head.Add(i, new RenderMesh {
                    mesh = headMesh,
                    material = arcColorMaterialInstance
                });
                height.Add(i, new RenderMesh {
                    mesh = heightMesh,
                    material = heightIndicatorColorMaterialInstance
                });
            }

            shadow = shadowRenderMesh;
            shadowGrayout = shadowGrayoutRenderMesh;

            //CREATE ENTITIES
            var arcs = parser.Arcs;

            arcs.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });

            var connectedArcsIdEndpoint = new List<ArcEndpointData>();
            var startTimesById = new List<int>();

            foreach (ArcRaw arc in arcs)
            {
                int startGroupTime = default;

                //Precalc and assign a connected arc id to avoid having to figure out connection during gameplay
                //placed into a new block to prevent data from being used later on
                ArcEndpointData arcStartPoint = (arc.timingGroup, arc.timing, arc.startX, arc.startY, arc.color);
                ArcEndpointData arcEndPoint = (arc.timingGroup, arc.endTiming, arc.endX, arc.endY, arc.color);

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
                    CreateHeadSegment(arc, head[arc.color], arcId);
                }

                if (isHeadArc || arc.startY != arc.endY)
                {
                    CreateHeightIndicator(arc, height[arc.color]);
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
                    CreateSegment(initial[arc.color], tstart, tend, arc.timingGroup, arc.timing, arc.timing, arcId);
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
                        PlayManager.Conductor.GetFloorPositionFromTiming(arc.timing, arc.timingGroup)
                    );

                for (int i = 0; i < segmentCount - 1; i++)
                {
                    int t = (int)((i + 1) * segmentLength);

                    fromTiming = toTiming;
                    toTiming = arc.timing + t;

                    start = end;
                    end = new float3(
                        Conversion.GetWorldX(Conversion.GetXAt((float)t / duration, arc.startX, arc.endX, arc.easing)),
                        Conversion.GetWorldY(Conversion.GetYAt((float)t / duration, arc.startY, arc.endY, arc.easing)),
                        PlayManager.Conductor.GetFloorPositionFromTiming(toTiming, arc.timingGroup)
                    );

                    CreateSegment(initial[arc.color], start, end, arc.timingGroup, fromTiming, toTiming, arcId);
                }

                fromTiming = toTiming;
                toTiming = arc.endTiming;

                start = end;
                end = new float3(
                    Conversion.GetWorldX(arc.endX),
                    Conversion.GetWorldY(arc.endY),
                    PlayManager.Conductor.GetFloorPositionFromTiming(arc.endTiming, arc.timingGroup)
                );

                CreateSegment(initial[arc.color], start, end, arc.timingGroup, fromTiming, toTiming, arcId);
                CreateJudgeEntity(arc, arc.color, startGroupTime, startBpm);

            }

            List<IIndicator> indicatorList = new List<IIndicator>(connectedArcsIdEndpoint.Count);

            foreach (var groupIdEndPoint in connectedArcsIdEndpoint)
            {
                ArcIndicator indicator = new ArcIndicator(Object.Instantiate(arcApproachIndicatorPrefab), Object.Instantiate(arcParticlePrefab), groupIdEndPoint.time);
                indicatorList.Add(indicator);
            }
            PlayManager.ArcIndicatorManager.Initialize(indicatorList);
        }

        private void CreateSegment(RenderMesh renderMesh, float3 start, float3 end, int timingGroup, int timing, int endTiming, int groupId)
        {
            Entity arcInstEntity = em.Instantiate(arcNoteEntityPrefab);
            em.SetSharedComponentData<RenderMesh>(arcInstEntity, renderMesh); 

            em.SetComponentData(arcInstEntity, new FloorPosition(start.z));

            em.SetComponentData(arcInstEntity, new TimingGroup(timingGroup));

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
            em.SetComponentData(arcInstEntity, ltwArc);

            em.SetComponentData(arcInstEntity, new BaseOffset(new float4(start.x, start.y, 0, 0)));
            em.SetComponentData(arcInstEntity, new BaseShear(new float4(dx, dy, dz, 0)));

            em.SetComponentData(arcInstEntity, new Cutoff(false));


            int t1 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(start.z + Constants.RenderFloorPositionRange, timingGroup);
            int t2 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(end.z - Constants.RenderFloorPositionRange, timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;

            em.SetComponentData(arcInstEntity, new AppearTime(appearTime));
            em.SetComponentData(arcInstEntity, new DestroyOnTiming(endTiming + Constants.HoldLostWindow));
            em.SetComponentData(arcInstEntity, new ArcGroupID(groupId));
            em.SetComponentData(arcInstEntity, new ChartTime(timing));
            em.SetComponentData(arcInstEntity, new ChartEndTime(endTiming));

            if (timing < endTiming)
            {
                Entity arcShadowEntity = em.Instantiate(arcShadowEntityPrefab);
                em.SetComponentData(arcShadowEntity, new FloorPosition(start.z));
                em.SetComponentData(arcShadowEntity, new TimingGroup(timingGroup));
                em.SetSharedComponentData<RenderMesh>(arcShadowEntity, shadowRenderMesh);
                LocalToWorld ltwShadow = new LocalToWorld()
                {
                    Value = new float4x4(
                        1, 0, dx, start.x,
                        0, 1, 0, 0,
                        0, 0, dz, 0,
                        0, 0, 0, 1
                    )
                };
                em.SetComponentData(arcShadowEntity, new BaseOffset(new float4(start.x, 0, 0, 0)));
                em.SetComponentData(arcShadowEntity, new BaseShear(new float4(dx, 0, dz, 0)));
                em.SetComponentData(arcShadowEntity, new Cutoff(false));
                em.SetComponentData(arcShadowEntity, ltwShadow);
                em.SetComponentData(arcShadowEntity, new AppearTime(appearTime));
                em.SetComponentData(arcShadowEntity, new DestroyOnTiming(endTiming + Constants.HoldLostWindow));
                em.SetComponentData(arcShadowEntity, new ArcGroupID(groupId));
                em.SetComponentData(arcShadowEntity, new ChartTime(timing));
            }
        }

        private void CreateHeightIndicator(ArcRaw arc, RenderMesh renderMesh)
        {
            Entity heightEntity = em.Instantiate(heightIndicatorEntityPrefab);

            float height = Conversion.GetWorldY(arc.startY) - 0.45f;

            float x = Conversion.GetWorldX(arc.startX); 
            float y = height / 2;
            const float z = 0;

            const float scaleX = 2.34f;
            float scaleY = height;
            const float scaleZ = 1;

            em.SetSharedComponentData(heightEntity, renderMesh);

            em.SetComponentData(heightEntity, new Translation()
            {
                Value = new float3(x, y, z)
            });
            em.AddComponentData(heightEntity, new NonUniformScale()
            {
                Value = new float3(scaleX, scaleY, scaleZ)
            });
            float floorpos = PlayManager.Conductor.GetFloorPositionFromTiming(arc.timing, arc.timingGroup);
            em.AddComponentData(heightEntity, new FloorPosition(floorpos));
            em.SetComponentData(heightEntity, new TimingGroup(arc.timingGroup));

            int t1 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, arc.timingGroup);
            int t2 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, arc.timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;

            em.SetComponentData(heightEntity, new AppearTime(appearTime));
            em.SetComponentData(heightEntity, new DestroyOnTiming(arc.timing));
        }

        private void CreateHeadSegment(ArcRaw arc, RenderMesh renderMesh, int groupID)
        {
            Entity headEntity = em.Instantiate(headArcNoteEntityPrefab);

            em.SetSharedComponentData(headEntity, renderMesh);

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
            em.SetComponentData(headEntity, new DestroyOnTiming(arc.timing));
            em.SetComponentData(headEntity, new ArcGroupID(groupID));
        }

        private void CreateJudgeEntity(ArcRaw arc, int colorId, int groupId, float startBpm)
        {

            Entity en = em.Instantiate(arcJudgeEntityPrefab);

            em.SetComponentData(en, ChartIncrTime.FromBpm(arc.timing, arc.endTiming, startBpm, out int comboCount));

            PlayManager.ScoreHandler.tracker.noteCount += comboCount;

            //very stupid
            em.SetComponentData(en, new ChartTime(arc.timing + Constants.LostWindow));
            em.SetSharedComponentData(en, new ArcColorID(colorId));
            em.SetComponentData(en,
                new ArcData(
                    Conversion.GetWorldPos(math.float2(arc.startX, arc.startY)),
                    Conversion.GetWorldPos(math.float2(arc.endX, arc.endY)),
                    arc.timing,
                    arc.endTiming,
                    arc.easing
                ));

            em.SetComponentData(en, new ArcGroupID(groupId));
            em.SetComponentData(en, new DestroyOnTiming(arc.endTiming + Constants.HoldLostWindow));
            
        }

        /// <summary>
        /// Stores data requried to handle arc endpoints.
        /// </summary>
        public struct ArcEndpointData
        {
            public int timingGroup;
            public int time;
            public float x;
            public float y;
            public int color;

            public ArcEndpointData(int timingGruop, int time, float x, float y, int color)
            {
                this.timingGroup = timingGruop;
                this.time = time;
                this.x = x;
                this.y = y;
                this.color = color; 
            }

            public override bool Equals(object obj)
            {
                return obj is ArcEndpointData other &&
                       timingGroup == other.timingGroup &&
                       time == other.time &&
                       x == other.x &&
                       y == other.y &&
                       color == other.color;
            }

            public static bool operator ==(ArcEndpointData l, ArcEndpointData r) => l.Equals(r);
            public static bool operator !=(ArcEndpointData l, ArcEndpointData r) => !(l == r);

            public override int GetHashCode()
            {
                int hashCode = 1052165582;
                hashCode = hashCode * -1521134295 + timingGroup.GetHashCode();
                hashCode = hashCode * -1521134295 + time.GetHashCode();
                hashCode = hashCode * -1521134295 + x.GetHashCode();
                hashCode = hashCode * -1521134295 + y.GetHashCode();
                hashCode = hashCode * -1521134295 + color.GetHashCode();
                return hashCode;
            }

            public void Deconstruct(out int timingGroup, out int time, out float x, out float y, out float color)
            {
                timingGroup = this.timingGroup;
                time = this.time;
                x = this.x;
                y = this.y;
                color = this.color;
            }

            public static implicit operator (int, int time, float, float, int)(ArcEndpointData value)
            {
                return (value.timingGroup, value.time, value.x, value.y, value.color);
            }

            public static implicit operator ArcEndpointData((int, int time, float, float, int) value)
            {
                return new ArcEndpointData(value.Item1, value.time, value.Item3, value.Item4, value.Item5);
            }
        }
    }
}