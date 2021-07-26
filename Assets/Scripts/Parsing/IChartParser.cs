using System.Collections.Generic;

namespace ArcCore.Parsing
{
    using ArcCore.Gameplay.Behaviours;
    using Data;

    public interface IChartParser
    {
        void Execute();
        List<TimingRaw> Timings { get; }
        List<TapRaw> Taps { get; }
        List<HoldRaw> Holds { get; }
        List<ArcRaw> Arcs { get; }
        List<TraceRaw> Traces { get; }
        List<ArctapRaw> Arctaps { get; }
        List<CameraEvent> Cameras { get; }
        int ChartOffset { get; }
        List<TimingGroupFlag> TimingGroupFlags { get; }
        List<int> UsedArcColors { get; }
        List<BaseSCObject> SceneControlObjects { get; }
    }
}
