#define DEFAULT_CHART

using ArcCore.Gameplay.Behaviours;
using ArcCore.Gameplay.Data;
using ArcCore.Gameplay.Objects.Particle;
using Unity.Entities;
using UnityEngine;
using System.Collections.Generic;
using ArcCore.Gameplay.EntityCreation;
using ArcCore.Parsing;
using Unity.Rendering;
using Unity.Collections;

namespace ArcCore.Gameplay
{
    public class PlayManager : MonoBehaviour
    {
        private static PlayManager instance;

        [Header("Behaviours")]
        public Conductor conductor;
        public ScoreHandler scoreHandler;
        public InputHandler inputHandler;
        public GameplayCamera gameplayCamera;
        public ScenecontrolHandler scenecontrolHandler;
        public ParticlePool particlePool;
        
        public static Conductor Conductor => instance.conductor;
        public static ScoreHandler ScoreHandler => instance.scoreHandler;
        public static InputHandler InputHandler => instance.inputHandler;
        public static GameplayCamera GameplayCamera => instance.gameplayCamera;
        public static ParticlePool ParticlePool => instance.particlePool;

        [Header("Beatline Creation Information")]
        public GameObject beatlinePrefab;

        public BeatlineEntityCreator GetBeatlineEntityCreator()
            => new BeatlineEntityCreator(world, beatlinePrefab);

        [Header("Tap Creation Information")]
        public GameObject tapNotePrefab;

        [Header("Arctap Creation Information")]
        public GameObject arcTapNotePrefab;
        public GameObject connectionLinePrefab;
        public GameObject shadowPrefab;

        public TapEntityCreator GetTapEntityCreator()
            => new TapEntityCreator(world, tapNotePrefab, arcTapNotePrefab, connectionLinePrefab, shadowPrefab);

        [Header("Hold Creation Information")]
        public GameObject holdNotePrefab;

        public HoldEntityCreator GetHoldEntityCreator()
            => new HoldEntityCreator(world, holdNotePrefab);

        [Header("Arc Creation Information")]
        public GameObject arcNotePrefab;
        public GameObject headArcNotePrefab;
        public GameObject heightIndicatorPrefab;
        public GameObject arcShadowPrefab;
        public GameObject arcJudgePrefab;
        public GameObject arcApproachIndicatorPrefab;
        public GameObject arcParticlePrefab;
        public Material arcMaterial;
        public Material heightMaterial;
        public Color redColor;
        public Mesh arcMesh;
        public Mesh arcHeadMesh;

        public ArcEntityCreator GetArcEntityCreator()
            => new ArcEntityCreator(
                world, arcNotePrefab, headArcNotePrefab,
                heightIndicatorPrefab, arcShadowPrefab, arcJudgePrefab,
                arcApproachIndicatorPrefab, arcParticlePrefab, redColor);

        [Header("Trace Creation Information")]
        public GameObject traceNotePrefab;
        public GameObject headTraceNotePrefab;
        public GameObject traceShadowPrefab;
        public GameObject traceApproachIndicatorPrefab;
        public Material traceMaterial;
        public Material traceShadowMaterial;
        public Mesh traceMesh;
        public Mesh traceHeadMesh;
        public Mesh traceShadowMesh;

        [Header("Indicator Management")]
        public IndicatorHandler arcIndicatorManager;
        public IndicatorHandler traceIndicatorManager;

        public static IndicatorHandler ArcIndicatorManager => instance.arcIndicatorManager;
        public static IndicatorHandler TraceIndicatorManager => instance.traceIndicatorManager;

        public TraceEntityCreator GetTraceEntityCreator()
            => new TraceEntityCreator(
                world, traceNotePrefab, headTraceNotePrefab, traceApproachIndicatorPrefab,
                traceShadowPrefab, traceMaterial, traceShadowMaterial,
                traceMesh, traceHeadMesh, traceShadowMesh);

        public static int ReceptorTime => instance.conductor.receptorTime;
        public static bool IsActive => instance != null;
        public static bool IsUpdating
        {
            get => instance.isUpdating;
            set => instance.isUpdating = value;
        }
        public static bool IsUpdatingAndActive => IsUpdating && IsActive;

        public static ParticleBuffer ParticleBuffer => instance.particleBuffer;
    
        //TODO: MAYBE MOVE THIS TO A SEPARATE SKIN OBJECT?
        //Hold skin data
        public static RenderMesh HoldHighlightRenderMesh => instance.holdHighlightRenderMesh;
        public static RenderMesh HoldGrayoutRenderMesh => instance.holdGrayoutRenderMesh;
        public static RenderMesh HoldInitialRenderMesh => instance.holdInitialRenderMesh;

        private ParticleBuffer particleBuffer;
        private RenderMesh holdHighlightRenderMesh;
        private RenderMesh holdGrayoutRenderMesh;
        private RenderMesh holdInitialRenderMesh;

        private NativeArray<GroupState> arcGroupHeldState;
        private List<ArcColorFSM> arcColorFsm;
        private int maxArcColor;

        public static NativeArray<GroupState> ArcGroupHeldState => instance.arcGroupHeldState;
        public static List<ArcColorFSM> ArcColorFsm => instance.arcColorFsm;
        public static int MaxArcColor => instance.maxArcColor;

        //Arc skin data
        public static (RenderMesh, RenderMesh, RenderMesh, RenderMesh, RenderMesh) GetRenderMeshVariants(int color)
            => (instance.arcInitialRenderMeshes[color],
                instance.arcHighlightRenderMeshes[color],
                instance.arcGrayoutRenderMeshes[color],
                instance.archeadRenderMeshes[color],
                instance.arcHeightRenderMeshes[color]);

        private Dictionary<int, RenderMesh> arcInitialRenderMeshes = new Dictionary<int, RenderMesh>();
        private Dictionary<int, RenderMesh> arcHighlightRenderMeshes = new Dictionary<int, RenderMesh>();
        private Dictionary<int, RenderMesh> arcGrayoutRenderMeshes = new Dictionary<int, RenderMesh>();
        private Dictionary<int, RenderMesh> archeadRenderMeshes = new Dictionary<int, RenderMesh>();
        private Dictionary<int, RenderMesh> arcHeightRenderMeshes = new Dictionary<int, RenderMesh>();

        public static RenderMesh ArcShadowRenderMesh => instance.arcShadowRenderMesh;
        public static RenderMesh ArcShadowGrayoutRenderMesh => instance.arcShadowGrayoutRenderMesh;

        private RenderMesh arcShadowRenderMesh;
        private RenderMesh arcShadowGrayoutRenderMesh;

        public static EntityCommandBuffer CommandBuffer => instance.commandBuffer;
        private EntityCommandBuffer commandBuffer;

        private World world;

        private bool isUpdating;
        public void Start()
        {
            instance = this;
            isUpdating = false;

#if DEFAULT_CHART
            LoadChart(Constants.GetDefaultArcChart());
            PlayMusic();
#endif
        }

        public static void LoadChart(string chart) => instance.LoadChartInstance(chart);
        private void LoadChartInstance(string chart)
        {
            var parser = new ArcParser(chart.Split('\n'));
            parser.Execute();

            world = World.DefaultGameObjectInjectionWorld;

            conductor.SetupTimingGroups(parser);
            gameplayCamera.SetupCamera(parser);
            scenecontrolHandler.CreateObjects(parser);

            GetArcEntityCreator().CreateEntitiesAndGetMeshes(
                parser,
                out arcInitialRenderMeshes,
                out arcHighlightRenderMeshes,
                out arcGrayoutRenderMeshes,
                out archeadRenderMeshes,
                out arcHeightRenderMeshes,
                out arcShadowRenderMesh,
                out arcShadowGrayoutRenderMesh
            );
            GetHoldEntityCreator().CreateEntitiesAndGetMeshes(
                parser,
                out holdHighlightRenderMesh,
                out holdGrayoutRenderMesh,
                out holdInitialRenderMesh
            );

            GetBeatlineEntityCreator().CreateEntities(parser);
            GetTapEntityCreator().CreateEntities(parser);
            GetTraceEntityCreator().CreateEntities(parser);

            maxArcColor = parser.MaxArcColor;

            arcGroupHeldState = new NativeArray<GroupState>(maxArcColor, Allocator.Persistent);
            arcColorFsm = new List<ArcColorFSM>();
            for (int i = 0; i < maxArcColor; i++)
            {
                arcColorFsm.Add(new ArcColorFSM(i));
            }
        }

        public static void PlayMusic() => instance.PlayMusicInstance();
        private void PlayMusicInstance()
        {
            conductor.PlayMusic();

            IsUpdating = true;
        }

        public static void CreateBuffer() => instance.CreateBufferInstance();
        private void CreateBufferInstance()
        {
            commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
        }

        public static void PlaybackBuffer() => instance.PlaybackBufferInstance();
        private void PlaybackBufferInstance()
        {
            if (commandBuffer.IsCreated)
            { 
                if(commandBuffer.ShouldPlayback) 
                    commandBuffer.Playback(world.EntityManager);
                commandBuffer.Dispose();
            }
        }

        public static void CreateParticleBuffer() => instance.CreateParticleBufferInstance();
        private void CreateParticleBufferInstance()
        {
            particleBuffer = new ParticleBuffer(Allocator.TempJob);
        }

        public static void PlaybackParticleBuffer() => instance.PlaybackParticleBufferInstance();
        private void PlaybackParticleBufferInstance()
        {
            if (particleBuffer.IsCreated)
            {
                particleBuffer.Playback();
                particleBuffer.Dispose();
            }
        }

        void OnDestroy()
        {
            instance = null;
            arcGroupHeldState.Dispose();
        }

        public void Update()
        {
            if (!IsUpdating) return;

            Debug.Log("tst");
        }
    }
}