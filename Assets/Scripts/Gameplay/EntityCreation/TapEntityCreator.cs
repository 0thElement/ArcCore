using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using ArcCore.Gameplay.Parsing.Data;
using ArcCore.Gameplay.Parsing;
using ArcCore.Utilities;
using ArcCore.Utilities.Extensions;

namespace ArcCore.Gameplay.EntityCreation
{
    public class TapEntityCreator
    {
        private Entity tapNoteEntityPrefab;
        private Entity arcTapNoteEntityPrefab;
        private Entity connectionLineEntityPrefab;
        private Entity shadowEntityPrefab;
        private EntityManager em;
        private IChartParser parser;

        public TapEntityCreator(
            World world,
            GameObject tapNotePrefab,
            GameObject arcTapNotePrefab,
            GameObject connectionLinePrefab,
            GameObject shadowPrefab)
        {
            em = world.EntityManager;
            var gocs = GameObjectConversionSettings.FromWorld(world, null);

            tapNoteEntityPrefab        = gocs.ConvertToNote(tapNotePrefab, em);
            arcTapNoteEntityPrefab     = gocs.ConvertToNote(arcTapNotePrefab, em);
            connectionLineEntityPrefab = gocs.ConvertToNote(connectionLinePrefab, em);
            shadowEntityPrefab         = gocs.ConvertToNote(shadowPrefab, em);
        }

        public void CreateEntities(IChartParser parser)
        {
            this.parser = parser;
            var taps    = parser.Taps;
            var arctaps = parser.Arctaps;

            taps   .Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });
            arctaps.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });
            
            int tapCount    = taps.Count;
            int arcTapCount = arctaps.Count;
            int tapIndex    = 0;
            int arcTapIndex = 0;

            while (tapIndex < tapCount && arcTapIndex < arcTapCount) 
            {
                TapRaw tap = taps[tapIndex];
                ArctapRaw arctap = arctaps[arcTapIndex];

                if (tap.timing == arctap.timing && tap.timingGroup == arctap.timingGroup)
                {
                    Entity tapEntity    = CreateTapEntity(tap);
                    Entity arctapEntity = CreateArcTapEntity(arctap);
                    CreateConnection(tap, arctap, tapEntity, arctapEntity);

                    tapIndex++;
                    arcTapIndex++;
                }
                else if (tap.timing < arctap.timing)
                {
                    CreateTapEntity(tap);
                    tapIndex++;
                }
                else
                {
                    CreateArcTapEntity(arctap);
                    arcTapIndex++;
                }
            }

            while (tapIndex < tapCount)
                CreateTapEntity(taps[tapIndex++]);

            while (arcTapIndex < arcTapCount)
                CreateArcTapEntity(arctaps[arcTapIndex++]);

        }
        public Entity CreateTapEntity(TapRaw tap)
        {
            Entity tapEntity = em.Instantiate(tapNoteEntityPrefab);
            TimingGroupFlag flag = parser.GetTimingGroupFlag(tap.timingGroup);

            float x = Conversion.TrackToX(tap.track);
            const float y = 0;
            const float z = 0;

            float floorpos = PlayManager.Conductor.GetFloorPositionFromTiming(tap.timing, tap.timingGroup);

            int t1 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, tap.timingGroup);
            int t2 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, tap.timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;

            em.SetComponentData(tapEntity, new Translation(){ Value = new float3(x, y, z) });
            em.SetComponentData(tapEntity, new FloorPosition(floorpos));
            em.SetComponentData(tapEntity, new TimingGroup(tap.timingGroup));
            em.SetComponentData(tapEntity, new AppearTime(appearTime));
            em.SetComponentData(tapEntity, new ChartTime(tap.timing));
            em.SetComponentData(tapEntity, new ChartLane(tap.track));

            if (flag.HasFlag(TimingGroupFlag.Autoplay))
                em.AddComponent(tapEntity, typeof(Autoplay));

            if (flag.HasFlag(TimingGroupFlag.NoInput))
                em.AddComponent(tapEntity, typeof(NoInput));

            em.SetSharedComponentData(tapEntity, Skin.Instance.tapRenderMesh);

            PlayManager.ScoreHandler.tracker.noteCount++;
            
            return tapEntity;
        }

        public Entity CreateArcTapEntity(ArctapRaw arctap)
        {
            Entity tapEntity = em.Instantiate(arcTapNoteEntityPrefab);
            TimingGroupFlag flag = parser.GetTimingGroupFlag(arctap.timingGroup);

            float x = Conversion.GetWorldX(arctap.position.x);
            float y = Conversion.GetWorldY(arctap.position.y);
            const float z = 0;

            float floorpos = PlayManager.Conductor.GetFloorPositionFromTiming(arctap.timing, arctap.timingGroup);

            int t1 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(floorpos - Constants.RenderFloorPositionRange, arctap.timingGroup);
            int t2 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(floorpos + Constants.RenderFloorPositionRange, arctap.timingGroup);
            int appearTime = (t1 < t2) ? t1 : t2;

            em.SetComponentData(tapEntity, new Translation() { Value = new float3(x, y, z) });
            em.SetComponentData(tapEntity, new FloorPosition(floorpos));
            em.SetComponentData(tapEntity, new TimingGroup(arctap.timingGroup));
            em.SetComponentData(tapEntity, new AppearTime(appearTime));
            em.SetComponentData(tapEntity, new ChartTime(arctap.timing));
            em.SetComponentData(tapEntity, new ChartPosition(Conversion.GetWorldPos(arctap.position)));

            if (flag.HasFlag(TimingGroupFlag.Autoplay))
                em.AddComponent(tapEntity, typeof(Autoplay));

            if (flag.HasFlag(TimingGroupFlag.NoInput))
                em.AddComponent(tapEntity, typeof(NoInput));

            em.SetSharedComponentData(tapEntity, Skin.Instance.arctapRenderMesh);

            if (!flag.HasFlag(TimingGroupFlag.NoShadow))
            {
                Entity shadowEntity = em.Instantiate(shadowEntityPrefab);
                em.SetComponentData(tapEntity, new ArcTapShadowReference(shadowEntity));
                em.SetComponentData(shadowEntity, new TimingGroup(arctap.timingGroup));
                em.SetComponentData(shadowEntity, new Translation() { Value = new float3(x, 0, z) });
                em.SetComponentData(shadowEntity, new FloorPosition(floorpos));
            }

            PlayManager.ScoreHandler.tracker.noteCount++;

            return tapEntity;
        }

        public void CreateConnection(TapRaw tap, ArctapRaw arctap, Entity tapEntity, Entity arcTapEntity)
        {
            Entity lineEntity = em.Instantiate(connectionLineEntityPrefab);
            TimingGroupFlag flag = parser.GetTimingGroupFlag(tap.timingGroup);

            float floorpos = PlayManager.Conductor.GetFloorPositionFromTiming(arctap.timing, arctap.timingGroup);

            float x = Conversion.GetWorldX(arctap.position.x);
            float y = Conversion.GetWorldY(arctap.position.y) - 0.5f;
            const float z = 0;

            float dx = Conversion.TrackToX(tap.track) - x;
            float dy = -y;

            float3 direction = new float3(dx, dy, 0);
            float length = math.sqrt(dx*dx + dy*dy);

            int appearTime = em.GetComponentData<AppearTime>(tapEntity).value;

            em.SetComponentData(lineEntity, new Translation(){ Value = new float3(x, y, z) });
            em.AddComponentData(lineEntity, new NonUniformScale(){ Value = new float3(1f, 1f, length) });
            em.SetComponentData(lineEntity, new Rotation(){ Value = quaternion.LookRotationSafe(direction, new Vector3(0,0,1)) });
            em.AddComponentData(lineEntity, new FloorPosition(floorpos));
            em.SetComponentData(lineEntity, new TimingGroup(arctap.timingGroup));
            em.SetComponentData(lineEntity, new AppearTime(appearTime));

            em.SetSharedComponentData(lineEntity, Skin.Instance.connectionLineRenderMesh);

            em.SetComponentData(tapEntity, new ConnectionReference(lineEntity));
            em.SetComponentData(arcTapEntity, new ConnectionReference(lineEntity));
        }
    }
}