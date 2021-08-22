#define DEFAULT_CHART

using ArcCore.Gameplay.Behaviours;
using ArcCore.Gameplay.Data;
using ArcCore.Gameplay.Systems;
using Unity.Entities;
using UnityEngine;
using System.Collections;
using ArcCore.Gameplay.EntityCreation;
using ArcCore.Parsing;
using Unity.Rendering;
using System.Linq;
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

        public TapEntityCreator GetTapEntityCreator()
            => new TapEntityCreator(world, tapNotePrefab);

        [Header("Hold Creation Information")]
        public GameObject holdNotePrefab;

        public HoldEntityCreator GetHoldEntityCreator()
            => new HoldEntityCreator(world, holdNotePrefab);

        [Header("Arctap Creation Information")]
        public GameObject arcTapNotePrefab;
        public GameObject connectionLinePrefab;
        public GameObject shadowPrefab;

        public ArcTapEntityCreator GetArcTapEntityCreator()
            => new ArcTapEntityCreator(
                world, arcTapNotePrefab,
                connectionLinePrefab, shadowPrefab);

        [Header("Arc Creation Information")]
        public GameObject arcNotePrefab;
        public GameObject headArcNotePrefab;
        public GameObject heightIndicatorPrefab;
        public GameObject arcShadowPrefab;
        public GameObject arcJudgePrefab;
        public Material arcMaterial;
        public Material heightMaterial;
        public Color redColor;
        public Mesh arcMesh;
        public Mesh arcHeadMesh;

        public ArcEntityCreator GetArcEntityCreator()
            => new ArcEntityCreator(
                world, arcNotePrefab, headArcNotePrefab,
                heightIndicatorPrefab, arcShadowPrefab, arcJudgePrefab,
                arcMaterial, heightMaterial, redColor, arcMesh,
                arcHeadMesh);

        [Header("Trace Creation Information")]
        public GameObject traceNotePrefab;
        public GameObject headTraceNotePrefab;
        public GameObject traceShadowPrefab;
        public Material traceMaterial;
        public Material traceShadowMaterial;
        public Mesh traceMesh;
        public Mesh traceHeadMesh;
        public Mesh traceShadowMesh;

        public TraceEntityCreator GetTraceEntityCreator()
            => new TraceEntityCreator(
                world, traceNotePrefab, headTraceNotePrefab,
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
        public static RenderMesh HighlightHold => instance.highlightHold;
        public static RenderMesh GrayoutHold => instance.grayoutHold;

        private ParticleBuffer particleBuffer;
        private RenderMesh highlightHold;
        private RenderMesh grayoutHold;

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

            GetArcEntityCreator().CreateEntities(parser);
            GetArcTapEntityCreator().CreateEntities(parser);
            GetBeatlineEntityCreator().CreateEntities(parser);
            GetHoldEntityCreator().CreateEntities(parser);
            GetTapEntityCreator().CreateEntities(parser);
            GetTraceEntityCreator().CreateEntities(parser);
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
        }

        public void Update()
        {
            if (!IsUpdating) return;

            Debug.Log("tst");
        }
    }
}