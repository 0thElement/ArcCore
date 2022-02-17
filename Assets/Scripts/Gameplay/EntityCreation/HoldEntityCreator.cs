using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using ArcCore.Gameplay.Parsing.Data;
using ArcCore.Gameplay.Parsing;
using ArcCore.Utilities;
using ArcCore.Utilities.Extensions;

namespace ArcCore.Gameplay.EntityCreation
{
    public class HoldEntityCreator
    {
        private Entity holdNoteEntityPrefab;
        private EntityManager em;

        public HoldEntityCreator(World world, GameObject holdNotePrefab)
        {
            em = world.EntityManager;
            var gocs = GameObjectConversionSettings.FromWorld(world, null);

            holdNoteEntityPrefab = gocs.ConvertToNote(holdNotePrefab, em);
        }

        public void CreateEntities(IChartParser parser)
        {
            var holds = parser.Holds;

            holds.Sort((item1, item2) => { return item1.timing.CompareTo(item2.timing); });

            foreach (HoldRaw hold in holds)
            {
                //Main entity
                Entity holdEntity = em.Instantiate(holdNoteEntityPrefab);
                TimingGroupFlag flag = parser.GetTimingGroupFlag(hold.timingGroup);

                float x = Conversion.TrackToX(hold.track);
                const float y = 0;
                const float z = 0;

                const float scalex = 1;
                const float scaley = 1;

                float endFloorPosition = PlayManager.Conductor.GetFloorPositionFromTiming(hold.endTiming, hold.timingGroup);
                float startFloorPosition = PlayManager.Conductor.GetFloorPositionFromTiming(hold.timing, hold.timingGroup);
                float scalez = - endFloorPosition + startFloorPosition;

                int t1 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(startFloorPosition + Constants.RenderFloorPositionRange, 0);
                int t2 = PlayManager.Conductor.GetFirstTimingFromFloorPosition(endFloorPosition - Constants.RenderFloorPositionRange, 0);
                int appearTime = (t1 < t2) ? t1 : t2;

                em.SetComponentData(holdEntity, new Translation(){ Value = new float3(x, y, z) });
                em.AddComponentData(holdEntity, new NonUniformScale(){ Value = new float3(scalex, scaley, scalez) });
                em.SetComponentData(holdEntity, new BaseLength(scalez));
                em.SetComponentData(holdEntity, new FloorPosition(startFloorPosition));
                em.SetComponentData(holdEntity, new TimingGroup(hold.timingGroup));
                em.SetComponentData(holdEntity, new ChartTime{value = hold.timing});
                em.SetComponentData(holdEntity, new ChartLane(hold.track));
                em.SetComponentData(holdEntity, new AppearTime(appearTime));
                em.SetComponentData(holdEntity, new DestroyOnTiming(hold.endTiming + Constants.FarWindow));

                if (flag.HasFlag(TimingGroupFlag.Autoplay))
                    em.AddComponent(holdEntity, typeof(Autoplay));

                if (flag.HasFlag(TimingGroupFlag.NoInput))
                    em.AddComponent(holdEntity, typeof(NoInput));

                em.SetSharedComponentData(holdEntity, Skin.Instance.holdInitialRenderMesh);

                float startBpm = PlayManager.Conductor.GetTimingEventFromTiming(hold.timing, hold.timingGroup).bpm;
                em.SetComponentData(holdEntity, ChartIncrTime.FromBpm(hold.timing, hold.endTiming, startBpm, out int comboCount));

                PlayManager.ScoreHandler.tracker.noteCount += comboCount;
            }
        }
    }

}