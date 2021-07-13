using ArcCore.Math;
using ArcCore.Gameplay.Utility;
using Unity.Mathematics;
using ArcCore.Gameplay;
using MoonSharp;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;

namespace ArcCore.Parsing
{
    using Data;
    public interface IChartParser
    {
        bool ExecuteFrame();
        List<TimingRaw> Timings { get; }
        List<TapRaw> Taps { get; }
        List<HoldRaw> Holds { get; }
        List<List<ArcRaw>> Arcs { get; }
        List<TraceRaw> Traces { get; }
        List<ArctapRaw> Arctaps { get; }
        List<CameraEvent> Cameras { get; }
        int ChartOffset { get; }
        List<TimingGroupFlags> TimingGroupFlags { get; }
    }


    [Serializable]
    public class ChartParserException : Exception
    {
        public ChartParserException() { }
        public ChartParserException(string message) : base(message) { }
        public ChartParserException(string message, Exception inner) : base(message, inner) { }
        protected ChartParserException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class ArcParser : IChartParser
    {
        public List<TimingRaw> Timings { get; private set; }
        public List<TapRaw> Taps { get; private set; }
        public List<HoldRaw> Holds { get; private set; }
        public List<List<ArcRaw>> Arcs { get; private set; }
        public List<TraceRaw> Traces { get; private set; }
        public List<ArctapRaw> Arctaps { get; private set; }
        public List<CameraEvent> Cameras { get; private set; }
        public int ChartOffset { get; private set; }
        public List<TimingGroupFlags> TimingGroupFlags { get; private set; }


        private string loadedChart;
        private int loadedIndex;

        public bool ExecuteFrame() => false;
    }
}
