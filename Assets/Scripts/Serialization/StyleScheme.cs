using ArcCore.Serialization.NewtonsoftExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcCore.Serialization
{
    [JsonHasPresets]
    public class StyleScheme : IEquatable<StyleScheme>
    {
        public TrackColor trackColor;
        public StyleChoice particle;
        public StyleChoice tap;
        public StyleChoice hold;
        public StyleChoice arctap;


        [JsonPreset]
        public static StyleScheme Light => new StyleScheme 
        {
            trackColor = new TrackColor { Value = TrackColor.Light },
            tap = StyleChoice.Light,
            arctap = StyleChoice.Light,
            hold = StyleChoice.Light,
            particle = StyleChoice.Light
        };
        [JsonPreset]
        public static StyleScheme Conflict => new StyleScheme
        {
            trackColor = new TrackColor { Value = TrackColor.Conflict },
            tap = StyleChoice.Conflict,
            arctap = StyleChoice.Conflict,
            hold = StyleChoice.Conflict,
            particle = StyleChoice.Conflict
        };

        public override bool Equals(object obj)
        {
            return obj is StyleScheme s && Equals(s);
        }
        public bool Equals(StyleScheme scheme)
        {
            return trackColor.Equals(scheme.trackColor) &&
                   particle == scheme.particle &&
                   tap == scheme.tap &&
                   hold == scheme.hold &&
                   arctap == scheme.arctap;
        }

        public override int GetHashCode()
        {
            int hashCode = -1980754000;
            hashCode = hashCode * -1521134295 + trackColor.GetHashCode();
            hashCode = hashCode * -1521134295 + particle.GetHashCode();
            hashCode = hashCode * -1521134295 + tap.GetHashCode();
            hashCode = hashCode * -1521134295 + hold.GetHashCode();
            hashCode = hashCode * -1521134295 + arctap.GetHashCode();
            return hashCode;
        }
    }
}