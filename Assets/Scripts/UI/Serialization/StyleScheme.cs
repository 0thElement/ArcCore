using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcCore.Serialization
{
    public class StyleScheme : IEquatable<StyleScheme>
    {
        public string background;
        public Color trackColor;
        public StyleChoice particle;
        public StyleChoice tap;
        public StyleChoice hold;
        public StyleChoice arctap;

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