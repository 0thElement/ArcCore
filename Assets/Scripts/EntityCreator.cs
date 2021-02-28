using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using Arcaoid.Utility;

public class EntityCreator : MonoBehaviour
{
    public static EntityCreator Instance { get; private set; }
    [SerializeField] private GameObject tapNotePrefab;
    [SerializeField] private GameObject holdNotePrefab;
    [SerializeField] private GameObject arcNotePrefab;
    [SerializeField] private GameObject traceNotePrefab;
    [SerializeField] private Material arcMaterial;
    [SerializeField] private Material traceMaterial;
    // [SerializeField] private GameObject arcTapNotePrefab;
    // [SerializeField] private GameObject tapToArcTapConnectLinePrefab;
    // [SerializeField] private GameObject arcHeightIndicatorPrefab;
    // [SerializeField] private GameObject arcHeadPrefab;
    // [SerializeField] private GameObject traceHeadPrefab;
    // [SerializeField] private GameObject beatlinePrefab;
    private Entity tapNoteEntityPrefab;
    private Entity holdNoteEntityPrefab;
    private Entity arcNoteEntityPrefab;
    private Entity traceNoteEntityPrefab;
    // private Entity arcTapNoteEntityPrefab;
    // private Entity tapToArcTapConnectLineEntityPrefab;
    // private Entity arcHeightIndicatorEntityPrefab;
    // private Entity arcHeadEntityPrefab;
    // private Entity traceHeadEntityPrefab;
    // private Entity beatlineEntityPrefab;

    private World defaultWorld;
    private EntityManager entityManager;

    private void Awake()
    {
        Instance = this;
        PrepareEntityPrefabConversion();
    }
    private void PrepareEntityPrefabConversion()
    {
        defaultWorld = World.DefaultGameObjectInjectionWorld;
        entityManager = defaultWorld.EntityManager;
        GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(defaultWorld, null);

        tapNoteEntityPrefab                = GameObjectConversionUtility.ConvertGameObjectHierarchy(tapNotePrefab, settings);
        holdNoteEntityPrefab               = GameObjectConversionUtility.ConvertGameObjectHierarchy(holdNotePrefab, settings);
        arcNoteEntityPrefab                = GameObjectConversionUtility.ConvertGameObjectHierarchy(arcNotePrefab, settings);
        traceNoteEntityPrefab              = GameObjectConversionUtility.ConvertGameObjectHierarchy(traceNotePrefab, settings);
        // arcTapNoteEntityPrefab             = GameObjectConversionUtility.ConvertGameObjectHierarchy(arcTapNotePrefab, settings);
        // tapToArcTapConnectLineEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(tapToArcTapConnectLinePrefab, settings);
        // arcHeightIndicatorEntityPrefab     = GameObjectConversionUtility.ConvertGameObjectHierarchy(arcHeightIndicatorPrefab, settings);
        // arcHeadEntityPrefab                = GameObjectConversionUtility.ConvertGameObjectHierarchy(arcHeadPrefab, settings);
        // traceHeadEntityPrefab              = GameObjectConversionUtility.ConvertGameObjectHierarchy(traceHeadPrefab, settings);
        // beatlineEntityPrefab               = GameObjectConversionUtility.ConvertGameObjectHierarchy(beatlinePrefab, settings);

        Mesh arcMesh = CreateBaseArcSegmentMesh(false);
        Mesh traceMesh = CreateBaseArcSegmentMesh(true);
        //Remove these component to allow direct access to localtoworld matrices
        //idk if this is a good way to set up an entity prefab in this case but this will do for now
        entityManager.RemoveComponent<Translation>(arcNoteEntityPrefab);
        entityManager.RemoveComponent<Rotation>(arcNoteEntityPrefab);
        entityManager.RemoveComponent<Scale>(arcNoteEntityPrefab);
        entityManager.SetSharedComponentData<RenderMesh>(arcNoteEntityPrefab, new RenderMesh(){
            mesh = CreateBaseArcSegmentMesh(false),
            material = arcMaterial
        });
        
        entityManager.RemoveComponent<Translation>(traceNoteEntityPrefab);
        entityManager.RemoveComponent<Rotation>(traceNoteEntityPrefab);
        entityManager.RemoveComponent<Scale>(traceNoteEntityPrefab);
        entityManager.SetSharedComponentData<RenderMesh>(traceNoteEntityPrefab, new RenderMesh(){
            mesh = CreateBaseArcSegmentMesh(false),
            material = traceMaterial
        });
    }
    public Mesh CreateBaseArcSegmentMesh(bool isTrace)
    {
        Vector3 fromPos = new Vector3(0, 0, 0);
        Vector3 toPos = new Vector3(0, 0, -1);
        float offset = isTrace ? 0.15f : 0.9f;

        Vector3[] vertices = new Vector3[6];
        Vector2[] uv = new Vector2[6];
        int[] triangles = new int[] { 0, 3, 2, 0, 2, 1, 0, 5, 4, 0, 4, 1 };

        vertices[0] = fromPos + new Vector3(0, offset / 2, 0);
        uv[0] = new Vector2();
        vertices[1] = toPos + new Vector3(0, offset / 2, 0);
        uv[1] = new Vector2(0, 1);
        vertices[2] = toPos + new Vector3(offset, -offset / 2, 0);
        uv[2] = new Vector2(1, 1);
        vertices[3] = fromPos + new Vector3(offset, -offset / 2, 0);
        uv[3] = new Vector2(1, 0);
        vertices[4] = toPos + new Vector3(-offset, -offset / 2, 0);
        uv[4] = new Vector2(1, 1);
        vertices[5] = fromPos + new Vector3(-offset, -offset / 2, 0);
        uv[5] = new Vector2(1, 0);

        return new Mesh(){
            vertices = vertices,
            uv = uv,
            triangles = triangles
        };
    }
    public void SetupTiming(List<List<AffTiming>> timingGroups)
    {
        //precalculate floorposition value for timing events
        //Unrolling the first loop. first one will also take on the job of creating beat divisor
        List<List<TimingEvent>> timingEventGroups = new List<List<TimingEvent>>(timingGroups.Count); 

        timingGroups[0].Sort( (item1, item2) => {return item1.timing.CompareTo(item2.timing);} );

        timingEventGroups.Add(new List<TimingEvent>(timingGroups[0].Count));

        timingEventGroups[0].Add(new TimingEvent(){
            timing = timingGroups[0][0].timing,
            floorPosition = 0,
            bpm = timingGroups[0][0].bpm
        });

        for (int i=1; i<timingGroups[0].Count; i++) 
        {
            timingEventGroups[0].Add(new TimingEvent(){
                timing = timingGroups[0][i].timing,
                floorPosition = timingGroups[0][i-1].bpm 
                              * (timingGroups[0][i].timing - timingGroups[0][i-1].timing)
                              + timingEventGroups[0][i-1].floorPosition,
                bpm = timingGroups[0][i].bpm
            });
        }
        //todo: beat divisor

        for (int i=1; i<timingGroups.Count; i++)
        {
            timingGroups[i].Sort( (item1, item2) => {return item1.timing.CompareTo(item2.timing);} );

            timingEventGroups.Add(new List<TimingEvent>(timingGroups[i].Count));

            timingEventGroups[i].Add(new TimingEvent(){
                timing = timingGroups[i][0].timing,
                floorPosition = 0,
                bpm = timingGroups[i][0].bpm
            });

            for (int j=1; j<timingGroups[i].Count; j++) 
            {
                timingEventGroups[i].Add(new TimingEvent(){
                    timing = timingGroups[i][j].timing,
                    floorPosition = timingGroups[i][j-1].bpm
                                  * (timingGroups[i][j].timing - timingGroups[i][j-1].timing)
                                  + timingEventGroups[i][j-1].floorPosition,
                    bpm = timingGroups[i][j].bpm
                });
            }
        }
        Conductor.Instance.SetTimingSetting(timingEventGroups);
    }

    public void CreateTapEntities(List<AffTap> affTapList)
    {
        foreach (AffTap tap in affTapList)
        {
            Entity tapEntity = entityManager.Instantiate(tapNoteEntityPrefab);

            float x = Convert.TrackToX(tap.track);
            float y = 0;
            float z = -200;
            entityManager.SetComponentData<Translation>(tapEntity, new Translation{ 
                Value = new float3(x, y, z)
            });
        }
    }
    public void CreateArcTapEntities(List<AffArcTap> affArcTapList)
    {
        // foreach (AffArcTap arctap in affArcTapList)
        // {
        //     Entity arcTapEntity = entityManager.Instantiate(arcTapNoteEntityPrefab);

        //     float x = arctap.position.x;
        //     float y = arctap.position.y;
        //     float z = -200;
        //     entityManager.SetComponentData<Translation>(arcTapEntity, new Translation{
        //         Value = new float3(x, y ,z)
        //     });

        // }
        //todo: tap to arctap connection line
    }
    public void CreateHoldEntities(List<AffHold> affHoldList)
    {
        foreach(AffHold hold in affHoldList)
        {
            Entity holdEntity = entityManager.Instantiate(holdNoteEntityPrefab);

            float x = Convert.TrackToX(hold.track);
            float y = 0;
            float z = -200;

            float scalex = 1.53f;
            float endFloorPosition = Conductor.Instance.GetFloorPositionFromTiming(hold.endTiming, hold.timingGroup);
            float startFloorPosition = Conductor.Instance.GetFloorPositionFromTiming(hold.timing, hold.timingGroup);
            float scaley = (endFloorPosition - startFloorPosition) / 3790f;
            float scalez = 1;

            entityManager.SetComponentData<Translation>(holdEntity, new Translation{
                Value = new float3(x, y, z)
            });
            entityManager.SetComponentData<NonUniformScale>(holdEntity, new NonUniformScale{
                Value = new float3(scalex, scaley, scalez)
            });
        }
        //todo: scale stuff idk
    }
    public void CreateArcEntities(List<List<AffArc>> affArcList)
    {
        foreach (List<AffArc> listByColor in affArcList)
        {
            List<float3> connectedArcsIdEndpoint = new List<float3>();
            foreach (AffArc arc in listByColor)
            {
                //Precalc and assign a connected arc id to avoid having to figure out connection during gameplay
                float3 arcStartPoint = new float3((float)arc.timing, arc.startX, arc.startY);
                float3 arcEndPoint = new float3((float)arc.endTiming, arc.endX, arc.endY);
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
                    //todo: create Archead and height indicator
                    arcId = connectedArcsIdEndpoint.Count;
                    connectedArcsIdEndpoint.Add(arcEndPoint);
                }

                //Generate arc segments and shadow segment(each segment is its own entity)
                
                int duration = arc.endTiming - arc.timing;
                int v1 = duration < 1000 ? 14 : 7;
                float v2 = 1f / (v1 * duration / 1000f);
                int segSize = (int)(duration * v2);
                int segmentCount = (segSize == 0 ? 0 : duration / segSize) + 1;

                Vector3 start;
                Vector3 end = new Vector3(
                    Convert.GetWorldX(arc.startX),
                    Convert.GetWorldY(arc.startY),
                    Conductor.Instance.GetFloorPositionFromTiming(arc.timing, arc.timingGroup)
                );

                for (int i=0; i<segmentCount - 1; i++)
                {
                    int t = (i + 1) * segSize;
                    start = end;
                    end = new Vector3(
                        Convert.GetWorldX(Convert.GetXAt((float)t/duration, arc.startX, arc.endX, arc.easing)),
                        Convert.GetWorldY(Convert.GetYAt((float)t/duration, arc.startY, arc.endY, arc.easing)),
                        Conductor.Instance.GetFloorPositionFromTiming(arc.timing + t, arc.timingGroup)
                    );
                    //todo: actually make an entity
                }

                start = end;
                end = new Vector3(
                    Convert.GetWorldX(arc.endX),
                    Convert.GetWorldY(arc.endY),
                    Conductor.Instance.GetFloorPositionFromTiming(arc.endTiming, arc.timingGroup)
                );
                //todo: actuallymake an entity
            }
        }
    }
}