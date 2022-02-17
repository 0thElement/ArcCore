using ArcCore.Gameplay.Components;
using ArcCore.Gameplay.Components.Tags;
using Unity.Entities;
using Unity.Collections;
using ArcCore.Gameplay.Data;
using Unity.Mathematics;
using ArcCore.Utilities;
using ArcCore.Gameplay.Utilities;

namespace ArcCore.Gameplay.Systems
{
    [UpdateInGroup(typeof(JudgementSystemGroup)), UpdateAfter(typeof(JudgeSetupSystem))]
    public class AutoplaySystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!PlayManager.IsUpdatingAndActive) return;

            ParticleBuffer particleBuffer = PlayManager.ParticleBuffer;
            EntityCommandBuffer commandBuffer = PlayManager.CommandBuffer;
            int currentTime = PlayManager.ReceptorTime;
            JudgeTracker tracker = PlayManager.ScoreHandler.tracker;
            var inputVisualFeedback = PlayManager.InputHandler.inputVisualFeedback;

            NativeArray<bool> trackAutoHeld = new NativeArray<bool>(new bool[4] { false, false, false, false }, Allocator.Temp);

            //Tap
            Entities.WithAll<Autoplay, WithinJudgeRange>().WithNone<NoInput, ChartIncrTime, ArcTapShadowReference>().ForEach(
                (Entity en, in ChartTime chartTime, in ChartLane chartLane) =>
                {
                    if (chartTime.value <= currentTime)
                    {
                        commandBuffer.DisableEntity(en);
                        commandBuffer.AddComponent<PastJudgeRange>(en);
                        tracker.AddJudge(JudgeType.MaxPure);
                        particleBuffer.PlayTapParticle(
                            new float2(Conversion.TrackToX(chartLane.lane), 0.5f),
                            JudgeType.MaxPure,
                            1f
                        );
                        trackAutoHeld[chartLane.lane - 1] = true;
                    }
                }
            ).Run();

            //Arctap
            Entities.WithAll<Autoplay, WithinJudgeRange>().WithNone<ChartIncrTime, NoInput>().ForEach(
                (Entity en, in ChartTime chartTime, in ChartPosition cp, in ArcTapShadowReference shadow) =>
                {
                    if (chartTime.value <= currentTime)
                    {
                        commandBuffer.DisableEntity(shadow.value);
                        commandBuffer.DisableEntity(en);
                        commandBuffer.AddComponent<PastJudgeRange>(en);

                        tracker.AddJudge(JudgeType.MaxPure);
                        particleBuffer.PlayTapParticle(
                            cp.xy,
                            JudgeType.MaxPure,
                            1f
                        );
                    }
                }
            ).Run();

            //Hold
            Entities.WithAll<Autoplay, WithinJudgeRange, HoldLocked>().WithNone<NoInput>().ForEach(
                (Entity en, ref ChartTime chartTime, in ChartLane cl) =>
                {
                    if (chartTime.value <= currentTime)
                    {
                        commandBuffer.RemoveComponent<HoldLocked>(en);
                        particleBuffer.PlayHoldParticle(
                            cl.lane - 1,
                            true
                        );
                    }
                }
            ).Run();

            Entities.WithAll<Autoplay, WithinJudgeRange>().WithNone<NoInput, HoldLocked, PastJudgeRange>().ForEach(
                (Entity en, ref ChartIncrTime chartIncrTime, in ChartLane lane) =>
                {
                    int count = chartIncrTime.UpdateJudgePointCache(currentTime);
                    if (count > 0)
                    {
                        tracker.AddJudge(JudgeType.MaxPure, count);
                        particleBuffer.PlayHoldParticle(lane.lane - 1, true);
                    }
                    trackAutoHeld[lane.lane - 1] = true;
                }
            ).Run();

            //Arc
            NativeArray<GroupState> arcGroupHeldState = PlayManager.ArcGroupHeldState;
            Entities.WithAll<Autoplay, WithinJudgeRange>().ForEach(
                (in ArcData arc, in ArcGroupID group) => {
                    if (arc.startTiming < currentTime)
                    {
                        arcGroupHeldState[group.value] = GroupState.Held;
                    }
                }
            ).Run();

            for (int i=0; i<4; i++)
                if (trackAutoHeld[i])
                    PlayManager.InputHandler.inputVisualFeedback.HighlightLane(i);

            trackAutoHeld.Dispose();

            PlayManager.ScoreHandler.tracker = tracker;
        }
    }
}