using System.Collections.Generic;

namespace ArcCore.Parsing
{
    using ArcCore.Gameplay.Behaviours;
    using Data;

    public interface IChartParser
    {
        void Execute();
        List<List<TimingRaw>> Timings { get; }
        List<TapRaw> Taps { get; }
        List<HoldRaw> Holds { get; }
        List<ArcRaw> Arcs { get; }
        List<ArcRaw> Traces { get; }
        List<ArctapRaw> Arctaps { get; }
        List<CameraEvent> Cameras { get; }
        List<int> CameraResets { get; }
        int ChartOffset { get; }
        List<TimingGroupFlag> TimingGroupFlags { get; }
        int MaxArcColor { get; }
        List<(ScenecontrolData, TextScenecontrolData)> TextScenecontrolData { get; }
        List<(ScenecontrolData, SpriteScenecontrolData)> SpriteScenecontrolData { get; }
    }
}
