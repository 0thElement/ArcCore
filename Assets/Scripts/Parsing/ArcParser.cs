using System;
using System.Collections.Generic;

namespace ArcCore.Parsing
{
    using ArcCore.Gameplay;
    using ArcCore.Gameplay.Behaviours;
    using ArcCore.Math;
    using ArcCore.Serialization;
    using ArcCore.Utilities;
    using Data;
    using System.IO;
    using System.Text;
    using Unity.Mathematics;
    using UnityEngine;

    public enum ArcParserState
    {
        None,
        Settings,
        Objects,
        Chart
    }

    public class ArcParser : CommandParser, IChartParser
    {
        public List<TimingRaw> Timings { get; private set; } = new List<TimingRaw>();
        public List<TapRaw> Taps { get; private set; } = new List<TapRaw>();
        public List<HoldRaw> Holds { get; private set; } = new List<HoldRaw>();
        public List<ArcRaw> Arcs { get; private set; } = new List<ArcRaw>();
        public List<TraceRaw> Traces { get; private set; } = new List<TraceRaw>();
        public List<ArctapRaw> Arctaps { get; private set; } = new List<ArctapRaw>();
        public List<CameraEvent> Cameras { get; private set; } = new List<CameraEvent>();
        public int ChartOffset { get; private set; }
        public List<TimingGroupFlag> TimingGroupFlags { get; private set; } = new List<TimingGroupFlag>();
        public HashSet<int> UsedArcColors { get; private set; } = new HashSet<int>();
        public List<BaseSCObject> SceneControlObjects { get; private set; } = new List<BaseSCObject>();

        public ArcParser(string[] lines): base(lines) {}

        private int timingGroup;
        private bool timingGroupHasTiming;
        private bool timingGroupHasTrace;
        private ArcParserState state;

        private Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        private Dictionary<string, BaseSCObject> objects = new Dictionary<string, BaseSCObject>();

        private Dictionary<string, List<ControlAxisKey>> xAxisKeys = new();
        private Dictionary<string, List<ControlAxisKey>> yAxisKeys = new();
        private Dictionary<string, List<ControlAxisKey>> zAxisKeys = new();

        private Dictionary<string, List<ControlAxisKey>> xRotAxisKeys = new();
        private Dictionary<string, List<ControlAxisKey>> yRotAxisKeys = new();
        private Dictionary<string, List<ControlAxisKey>> zRotAxisKeys = new();

        private Dictionary<string, List<ControlAxisKey>> xSclAxisKeys = new();
        private Dictionary<string, List<ControlAxisKey>> ySclAxisKeys = new();
        private Dictionary<string, List<ControlAxisKey>> zSclAxisKeys = new();

        private Dictionary<string, List<ControlAxisKey>> redKeys = new();
        private Dictionary<string, List<ControlAxisKey>> blueKeys = new();
        private Dictionary<string, List<ControlAxisKey>> greenKeys = new();
        private Dictionary<string, List<ControlAxisKey>> alphaKeys = new();

        private Dictionary<string, List<ControlValueKey<bool>>> enableKeys = new();

        private Dictionary<string, List<ControlValueKey<Sprite>>> imageKeys = new();
        private Dictionary<string, List<ControlValueKey<int>>> sortLayerKeys = new();

        private Dictionary<string, List<ControlValueKey<string>>> stringKeys = new();

        private bool TryGetTiminggroupFlag(string value, out TimingGroupFlag result)
        {
            switch(value)
            {
                case "no_input":
                    result = TimingGroupFlag.NoInput;
                    break;
                default:
                    result = TimingGroupFlag.None;
                    return false;
            }

            return true;
        }

        private bool TryGetArcEasing(string value, out ArcEasing result)
        {
            switch(value)
            {
                case "s":
                    result = ArcEasing.s;
                    break;
                case "b":
                    result = ArcEasing.b;
                    break;
                case "si":
                    result = ArcEasing.si;
                    break;
                case "so":
                    result = ArcEasing.so;
                    break;
                case "sisi":
                    result = ArcEasing.sisi;
                    break;
                case "siso":
                    result = ArcEasing.siso;
                    break;
                case "sosi":
                    result = ArcEasing.sosi;
                    break;
                case "soso":
                    result = ArcEasing.soso;
                    break;
                default:
                    result = ArcEasing.s;
                    return false;
            }
            return true;
        }

        private void NewTextObject()
        {
            var iden = GetValue("identifier");

            objects.Add(iden, new TextSCObject { startValue = GetStrValue() });

            xAxisKeys.Add(iden, new());
            yAxisKeys.Add(iden, new());
            zAxisKeys.Add(iden, new());
            xRotAxisKeys.Add(iden, new());
            yRotAxisKeys.Add(iden, new());
            zRotAxisKeys.Add(iden, new());
            xSclAxisKeys.Add(iden, new());
            ySclAxisKeys.Add(iden, new());
            zSclAxisKeys.Add(iden, new());
            redKeys.Add(iden, new());
            blueKeys.Add(iden, new());
            greenKeys.Add(iden, new());
            alphaKeys.Add(iden, new());
            stringKeys.Add(iden, new());
        }
        private void NewSpriteObject()
        {
            var iden = GetValue("identifier");
            var spr = GetValue("identifier");

            if(!sprites.ContainsKey(spr))
                throw GetParsingException($"Undeclared sprite cannot be used: \"{spr}\".");

            objects.Add(iden, new SpriteSCObject { startSprite = sprites[spr] });

            xAxisKeys.Add(iden, new());
            yAxisKeys.Add(iden, new());
            zAxisKeys.Add(iden, new());
            xRotAxisKeys.Add(iden, new());
            yRotAxisKeys.Add(iden, new());
            zRotAxisKeys.Add(iden, new());
            xSclAxisKeys.Add(iden, new());
            ySclAxisKeys.Add(iden, new());
            zSclAxisKeys.Add(iden, new());
            redKeys.Add(iden, new());
            blueKeys.Add(iden, new());
            greenKeys.Add(iden, new());
            alphaKeys.Add(iden, new());
            stringKeys.Add(iden, new());
        }

        private protected override CommandData GetCommandData(string command)
        {
            //message and predicate constants
            const string SettingsModeMsg = "Cannot use this command unless in settings mode.";
            bool SettingsMode() => (state == ArcParserState.Settings);

            const string ObjectsModeMsg = "Cannot use this command unless in objects mode.";
            bool ObjectsMode() => (state == ArcParserState.Objects);

            const string ChartModeMsg = "Cannot use this command unless in chart mode.";
            bool ChartMode() => (state == ArcParserState.Chart);

            const string ChartObjMsg = "Cannot create this chart item unless in chart mode and timing group has at least one timing.";
            bool ChartObjMode() => (state == ArcParserState.Chart && timingGroupHasTiming);

            const string ArctapMsg = "Cannot create this chart item unless in chart mode, timing group has at least one timing, and there is at least one trace in the current timinggroup.";
            bool Arctap() => ChartObjMode() && timingGroupHasTrace;

            //short methods
            void SetOffset() => ChartOffset = GetInt();

            //actual cases
            switch (command)
            {
                //mode commands
                case "settings":
                    return ("settings header", () => state = ArcParserState.Settings, 
                        "Cannot use a settings header more than once or after a chart or objects header.", () => state < ArcParserState.Settings);
                case "objects":
                    return ("objects header", () => state = ArcParserState.Objects,
                        "Cannot use an objects header more than once or after a chart header.", () => state < ArcParserState.Objects);
                case "chart":
                    return ("chart header", () => state = ArcParserState.Chart,
                        "Cannot use a chart header more than once.", () => state < ArcParserState.Chart);

                //settings commands
                case "audio_offset":
                    return ("audio offset setter", SetOffset, SettingsModeMsg, SettingsMode);

                //objects commands
                case "image":
                    return ("image object", NewImage, ObjectsModeMsg, ObjectsMode);
                case "text":
                    return ("text object", NewTextObject, ObjectsModeMsg, ObjectsMode);
                case "sprite":
                    return ("sprite object", NewSpriteObject, ObjectsModeMsg, ObjectsMode);

                //chart commands
                case "timing":
                    return (null, NewTiming, ChartModeMsg, ChartMode);
                case "tap":
                    return (null, NewTap, ChartObjMsg, ChartObjMode);
                case "hold":
                    return (null, NewHold, ChartObjMsg, ChartObjMode);
                case "arc":
                    return (null, NewArc, ChartObjMsg, ChartObjMode);
                case "trace":
                    return (null, NewTrace, ChartObjMsg, ChartObjMode);
                case "arctap":
                    return (null, NewArctap, ArctapMsg, Arctap);
                case "camera":
                    return (null, NewCamera, ChartObjMsg, ChartObjMode);
                case "keyframe":
                    return (null, NewKeyframe, ChartObjMsg, ChartObjMode);
                case "new_group":
                    return ("new timing group", ManageGroup, ChartModeMsg, ChartMode);
            }

            return null;
        }

        private protected override void OnExecutionEnd()
        {
            foreach(var kvp in xAxisKeys) 
            {
                objects[kvp.Key].xAxisKeys = kvp.Value.ToArray();
            }

            foreach (var kvp in yAxisKeys)
            {
                objects[kvp.Key].yAxisKeys = kvp.Value.ToArray();
            }

            foreach (var kvp in zAxisKeys)
            {
                objects[kvp.Key].zAxisKeys = kvp.Value.ToArray();
            }

            foreach (var kvp in xRotAxisKeys)
            {
                objects[kvp.Key].xRotAxisKeys = kvp.Value.ToArray();
            }

            foreach (var kvp in yRotAxisKeys)
            {
                objects[kvp.Key].yRotAxisKeys = kvp.Value.ToArray();
            }

            foreach (var kvp in zRotAxisKeys)
            {
                objects[kvp.Key].zRotAxisKeys = kvp.Value.ToArray();
            }

            foreach (var kvp in xSclAxisKeys)
            {
                objects[kvp.Key].xScaleAxisKeys = kvp.Value.ToArray();
            }

            foreach (var kvp in ySclAxisKeys)
            {
                objects[kvp.Key].yScaleAxisKeys = kvp.Value.ToArray();
            }

            foreach (var kvp in zSclAxisKeys)
            {
                objects[kvp.Key].zScaleAxisKeys = kvp.Value.ToArray();
            }

            foreach (var kvp in redKeys)
            {
                objects[kvp.Key].redKeys = kvp.Value.ToArray();
            }

            foreach (var kvp in greenKeys)
            {
                objects[kvp.Key].greenKeys = kvp.Value.ToArray();
            }

            foreach (var kvp in blueKeys)
            {
                objects[kvp.Key].blueKeys = kvp.Value.ToArray();
            }

            foreach (var kvp in alphaKeys)
            {
                objects[kvp.Key].alphaKeys = kvp.Value.ToArray();
            }

            foreach (var kvp in enableKeys)
            {
                objects[kvp.Key].enableKeys = kvp.Value.ToArray();
            }

            foreach (var kvp in enableKeys)
            {
                objects[kvp.Key].enableKeys = kvp.Value.ToArray();
            }

            foreach (var kvp in imageKeys)
            {
                (objects[kvp.Key] as SpriteSCObject).imageKeys = kvp.Value.ToArray();
            }

            foreach (var kvp in sortLayerKeys)
            {
                (objects[kvp.Key] as SpriteSCObject).sortLayerKeys = kvp.Value.ToArray();
            }

            foreach (var kvp in stringKeys)
            {
                (objects[kvp.Key] as TextSCObject).stringKeys = kvp.Value.ToArray();
            }
        }

        private int GetLane()
            => GetInt("lane", value => 0 < value && value < 5);

        private void NewImage()
        {
            var name = GetValue("identifier");
            var path = GetValue("path");
            path = FileManagement.GetRealPathFromUserInput(path) + ".png";

            Texture2D tex = new Texture2D(2,2);

            if (File.Exists(path))
            {
                var data = File.ReadAllBytes(path);
                if (!tex.LoadImage(data)) goto ERR;
            }
            else goto ERR;

            sprites[name] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one / 2);
            return;

            ERR:
            throw GetParsingException($"The provided path did not exist: \"{path}\".");
        }
        private void NewKeyframe()
        {
            EasingType GetEasing() => GetTypedValue<EasingType>("general easing type", Ease.TryParseEasingType);

            var obj = GetValue("identifier");

            if (!objects.ContainsKey(obj))
                throw GetParsingException($"Undeclared object: \"{obj}\".");

            bool isSprite = objects[obj] is SpriteSCObject;

            var type = GetValue("keyframe type");
            var timing = GetInt();

            void AppendNewAxis(List<ControlAxisKey> l) => l.Add(new ControlAxisKey { timing = timing, easing = GetEasing(), targetValue = GetFloat() });
            void RequireSprite()
            {
                if (!isSprite) throw GetParsingException($"Non-sprite object \"{obj}\" cannot have property \"{type}\" set.");
            }
            void RequireText()
            {
                if (isSprite) throw GetParsingException($"Non-text object \"{obj}\" cannot have property \"{type}\" set.");
            }

            switch (type)
            {
                case "x_pos":
                    AppendNewAxis(xAxisKeys[obj]);
                    break;
                case "y_pos":
                    AppendNewAxis(yAxisKeys[obj]);
                    break;
                case "z_pos":
                    AppendNewAxis(zAxisKeys[obj]);
                    break;
                case "x_rot":
                    AppendNewAxis(xRotAxisKeys[obj]);
                    break;
                case "y_rot":
                    AppendNewAxis(yRotAxisKeys[obj]);
                    break;
                case "z_rot":
                    AppendNewAxis(zRotAxisKeys[obj]);
                    break;
                case "x_scl":
                    AppendNewAxis(xSclAxisKeys[obj]);
                    break;
                case "y_scl":
                    AppendNewAxis(ySclAxisKeys[obj]);
                    break;
                case "z_scl":
                    AppendNewAxis(zSclAxisKeys[obj]);
                    break;
                case "red":
                    AppendNewAxis(redKeys[obj]);
                    break;
                case "green":
                    AppendNewAxis(greenKeys[obj]);
                    break;
                case "blue":
                    AppendNewAxis(blueKeys[obj]);
                    break;
                case "alpha":
                    AppendNewAxis(alphaKeys[obj]);
                    break;
                case "enable":
                    enableKeys[obj].Add(new ControlValueKey<bool> { timing = timing, value = GetBool() });
                    break;

                case "image":

                    RequireSprite();

                    var spr = GetValue("identifier");
                    if (!sprites.ContainsKey(spr))
                        throw GetParsingException($"Undeclared sprite cannot be used: \"{spr}\".");

                    imageKeys[obj].Add(new ControlValueKey<Sprite> { timing = timing, value = sprites[spr] });
                    break;

                case "sortLayer":

                    RequireSprite();
                    sortLayerKeys[obj].Add(new ControlValueKey<int> { timing = timing, value = GetInt() });
                    break;

                case "string":

                    RequireText();
                    stringKeys[obj].Add(new ControlValueKey<string> { timing = timing, value = GetStrValue() });
                    break;

                default:
                    throw GetParsingException($"Invalid keyframe type: {type}.");
            }
        }
        private void NewTiming()
        {
            Timings.Add(
                new TimingRaw
                {
                    timing = GetInt(),
                    bpm = GetFloat(),
                    divisor = GetFloat()
                }
            );
            timingGroupHasTiming = true;
        }
        private void NewTap()
        {
            Taps.Add(
                new TapRaw
                {
                    timing = GetInt(),
                    track = GetLane(),
                    timingGroup = timingGroup
                }
            );
        }
        private void NewHold()
        {
            int t;
            Holds.Add(
                new HoldRaw
                {
                    timing = t = GetInt(),
                    endTiming = GetInt(predicate: val => val >= t),
                    track = GetLane(),
                    timingGroup = timingGroup
                }
            );
        }
        private void NewArc()
        {
            int t, col;
            Arcs.Add(
                new ArcRaw
                {
                    timing = t = GetInt(),
                    endTiming = GetInt(predicate: val => val >= t),
                    startX = GetFloat(),
                    startY = GetFloat(),
                    endX = GetFloat(),
                    endY = GetFloat(),
                    easing = GetTypedValue<ArcEasing>("arc easing", TryGetArcEasing),
                    color = col = GetInt(predicate: val => val > 0),
                    timingGroup = timingGroup
                }
            );

            UsedArcColors.Add(col);
        }
        private void NewTrace()
        {
            int t;
            Traces.Add(
                new TraceRaw
                {
                    timing = t = GetInt(),
                    endTiming = GetInt(predicate: val => val >= t),
                    startX = GetFloat(),
                    startY = GetFloat(),
                    endX = GetFloat(),
                    endY = GetFloat(),
                    easing = GetTypedValue<ArcEasing>("arc easing", TryGetArcEasing),
                    timingGroup = timingGroup
                }
            );

            timingGroupHasTrace = true;
        }
        private void NewArctap()
        {
            int t;
            Arctaps.Add(
                new ArctapRaw
                {
                    timing = t = GetInt(),
                    position = Traces[Traces.Count - 1].GetPosAt(t),
                    timingGroup = timingGroup
                }
            );
        }
        private void NewCamera()
        {
            Cameras.Add(
                new CameraEvent
                {
                    Timing = GetInt(),
                    targetChange = new PosRot(new float3(GetFloat(), GetFloat(), GetFloat()), new float3(GetFloat(), GetFloat(), GetFloat()))
                }
            );
        }

        private void ManageGroup()
        {
            timingGroup++;
            TimingGroupFlag flag = TimingGroupFlag.None;

            while(GetTypedValue<TimingGroupFlag>(out var value, "timing group flag", TryGetTiminggroupFlag))
            {
                flag |= value;
            }

            TimingGroupFlags.Add(flag);

            timingGroupHasTiming = false;
            timingGroupHasTrace = false;
        }
    }
}
