﻿using ArcCore.Gameplay.Behaviours;
using ArcCore.Gameplay.Data;
using ArcCore.Gameplay.Objects.Particle;
using Unity.Entities;
using UnityEngine;
using System.Collections.Generic;
using ArcCore.Gameplay.EntityCreation;
using ArcCore.Gameplay.Parsing;
using Unity.Collections;
using ArcCore.Utilities;
using UnityEngine.UI;
using ArcCore.Storage.Data;
using ArcCore.Storage;
using System.IO;
using ArcCore.Gameplay.Components;
using ArcCore.Scenes;
using UnityEngine.Networking;
using System.Collections;

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

        [Header("Chart")]
        public Image background; 
        public Image jacket; 
        public Text title;
        public Text difficulty;
        public Image difficultyFill;


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
        public GameObject arcApproachIndicatorPrefab;
        public GameObject arcParticlePrefab;

        public ArcEntityCreator GetArcEntityCreator()
            => new ArcEntityCreator(
                world, arcNotePrefab, headArcNotePrefab,
                heightIndicatorPrefab, arcShadowPrefab,
                arcApproachIndicatorPrefab, arcParticlePrefab);


        [Header("Trace Creation Information")]
        public GameObject traceNotePrefab;
        public GameObject headTraceNotePrefab;
        public GameObject traceShadowPrefab;
        public GameObject traceApproachIndicatorPrefab;

        public TraceEntityCreator GetTraceEntityCreator()
            => new TraceEntityCreator(
                world, traceNotePrefab, headTraceNotePrefab,
                traceApproachIndicatorPrefab, traceShadowPrefab);

        public static int ReceptorTime => instance.conductor.receptorTime;
        public static bool IsActive => instance != null;
        public static bool IsUpdating
        {
            get => instance.isUpdating;
            set => instance.isUpdating = value;
        }
        public static bool ReadyToPlay 
        {
            get => instance.readyToPlay;
            set => instance.readyToPlay = value;
        }
        private bool readyToPlay;
        public static bool IsUpdatingAndActive => IsActive && IsUpdating;

        public static ParticleBuffer ParticleBuffer => instance.particleBuffer;
        private ParticleBuffer particleBuffer;

        public static int MaxArcColor => instance.maxArcColor;
        private int maxArcColor;

        public static NativeArray<GroupState> ArcGroupHeldState => instance.arcGroupHeldState;
        public static List<ArcColorFSM> ArcColorFsm => instance.arcColorFsm;
        private NativeArray<GroupState> arcGroupHeldState;
        private List<ArcColorFSM> arcColorFsm;

        private IndicatorHandler arcIndicatorHandler = new IndicatorHandler();
        private IndicatorHandler traceIndicatorHandler = new IndicatorHandler();

        public static IndicatorHandler ArcIndicatorHandler => instance.arcIndicatorHandler;
        public static IndicatorHandler TraceIndicatorHandler => instance.traceIndicatorHandler;

        public static EntityCommandBuffer CommandBuffer => instance.commandBuffer;
        private EntityCommandBuffer commandBuffer;

        private World world;

        [Header("Resuming")]
        public PlayPauseHandler playPauseHandler;

        [Header("Debug")]
        [SerializeField] private UnityEngine.UI.Text debugText;
        public static UnityEngine.UI.Text DebugText => instance.debugText;

        private bool isUpdating;
        public float blendStyle;

        private Level currentLevel;
        private Chart currentChart;

        public void Awake()
        {
            instance = this;
            isUpdating = false;
            readyToPlay = false;
            scoreHandler.ResetScores();
        }

        #region IO
        private void ReadChart(string chart, Style style)
        {
            var parser = new ArcParser(chart.Split('\n'));
            parser.Execute();

            Skin.Instance.ApplyStyle(style, parser.MaxArcColor, Settings.ArcColors);

            world = World.DefaultGameObjectInjectionWorld;

            conductor.chartOffset = parser.ChartOffset;
            conductor.SetupTimingGroups(parser);
            gameplayCamera.SetupCamera(parser);
            scenecontrolHandler.CreateObjects(parser);

            int arcGroupCount = 0;
            GetArcEntityCreator().CreateEntities(parser, out arcGroupCount);
            GetHoldEntityCreator().CreateEntities(parser);
            GetBeatlineEntityCreator().CreateEntities(parser);
            GetTapEntityCreator().CreateEntities(parser);
            GetTraceEntityCreator().CreateEntities(parser);

            
            arcGroupHeldState = new NativeArray<GroupState>(arcGroupCount, Allocator.Persistent);

            maxArcColor = parser.MaxArcColor;
            arcColorFsm = new List<ArcColorFSM>();
            for (int i = 0; i <= maxArcColor; i++)
            {
                arcColorFsm.Add(new ArcColorFSM(i));
            }
        }

        private void Clear()
        {
            scoreHandler.ResetScores();
            EntityManager em = world.EntityManager;
            em.DestroyEntity(em.UniversalQuery);
            arcGroupHeldState.Dispose();
            arcIndicatorHandler.Destroy();
            traceIndicatorHandler.Destroy();
            readyToPlay = false;
        }

        public static void ApplyChart(Level level, Chart chart) => instance.ApplyChartInstance(level, chart);
        public void ApplyChartInstance(Level level, Chart chart)
        {
            currentChart = chart;
            currentLevel = level;

            // Set information
            jacket.sprite = level.GetSprite(chart.ImagePath);
            background.sprite = level.GetSprite(chart.Background);
            title.text = chart.Name;
            difficulty.text = $"{chart.DifficultyGroup.Name}{(chart.DifficultyGroup.Name == "" ? "" : " ")}{chart.Difficulty}";
            difficultyFill.color = chart.DifficultyGroup.Color;

            // Read chart file
            string chartData = File.ReadAllText(level.GetRealPath(chart.ChartPath));

            StartCoroutine(LoadAudioAndChart(level.GetRealPath(chart.SongPath), chartData, chart.Style));
        }

        private IEnumerator LoadAudioAndChart(string path, string chartData, Style chartStyle, float startTime = 0)
        {
            SceneTransitionManager.Instance.LoadingText = "Loading audio file";
            SceneTransitionManager.Instance.KeepShutterClosed();

            using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.OGGVORBIS))
            {
                yield return req.SendWebRequest();
                if (!string.IsNullOrWhiteSpace(req.error))
                {
                    throw new System.Exception($"Cannot load audio file at {path}");
                }
                AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
                conductor.Initialize(clip, startTime);
            }

            SceneTransitionManager.Instance.LoadingText = "Loading chart file";

            ReadChart(chartData, chartStyle);
            conductor.UpdateCurrentFloorPosition();

            SceneTransitionManager.Instance.ReadyToOpenShutter();
            isUpdating = true;

            while (!readyToPlay) yield return null;
            PlayMusicInstance(startTime);
        }

        public static void LoadDefaultChart() => instance.LoadDefaultChartInstance();
        private void LoadDefaultChartInstance()
        {
            ReadChart(Constants.GetDefaultArcChart(), Style.Blend);
        }
        #endregion

        #region Play/Pause
        public static void Pause() => instance.PauseInstance();
        public static void Resume() => instance.ResumeInstance();
        public static void Restart() => instance.RestartInstance();
        public static void Quit() => instance.QuitInstance();
        public static void PlayMusic(float startTime = 0) => instance.PlayMusicInstance(startTime);
        private void PlayMusicInstance(float startTime)
        {
            conductor.PlayMusic(startTime);
            isUpdating = true;
        }

        private void PauseInstance()
        {
            conductor.PauseMusic();
            isUpdating = false;
        }

        private void ResumeInstance()
        {
            conductor.ResumeMusic();
            isUpdating = true;
        }

        private void RestartInstance()
        {
            SceneTransitionManager.Instance.OnShutterClose += () => {
                Clear();
                ApplyChartInstance(currentLevel, currentChart);
            };
            SceneTransitionManager.Instance.OnShutterOpen += () => {
                readyToPlay = true;
            };
            SceneTransitionManager.Instance.CloseShutterWithoutSwitchingScene();
        }

        private void QuitInstance()
        {
            Clear();
            SceneTransitionManager.Instance.ReturnFromPlayScene(null);
        }

        public static int GetResumeTimingHint()
        {
            int currentTime = ReceptorTime + Conductor.FullOffset;
            if (currentTime < 0) return 0; //Song not started

            EntityManager em = instance.world.EntityManager;
            EntityQueryDesc query = new EntityQueryDesc()
            {
                All = new ComponentType[] {typeof(ChartTime)}
            };

            NativeArray<ChartTime> chartTimes = em.CreateEntityQuery(query).ToComponentDataArray<ChartTime>(Allocator.Temp);
            if (chartTimes.Length == 0) return 0;

            int minTime = int.MaxValue;
            for (int i = 0; i < chartTimes.Length; i++)
            {
                int time = chartTimes[i].value;
                if (time < minTime && time > currentTime)
                    minTime = time;
            }

            chartTimes.Dispose();

            return minTime - currentTime;
        }
        #endregion

        #region Buffers
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
        #endregion

        void OnDestroy()
        {
            instance = null;
            // arcGroupHeldState.Dispose();
        }

        void Update()
        {
            //TEMPORARY
            Skin.Instance.BlendAll(blendStyle);
        }
    }
}