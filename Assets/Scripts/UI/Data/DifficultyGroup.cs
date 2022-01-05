using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArcCore.UI.Data
{
    public class DifficultyGroup : IEquatable<DifficultyGroup>
    {
        public Color Color { get; set; }
        public string Name { get; set; }
        public int Precedence { get; set; }

        public override bool Equals(object obj)
            => obj is DifficultyGroup group && Equals(group);
        public bool Equals(DifficultyGroup other)
            => Color == other.Color && Name == other.Name && Precedence == other.Precedence;
        public static bool operator ==(DifficultyGroup a, DifficultyGroup b)
            => a.Equals(b);
        public static bool operator !=(DifficultyGroup a, DifficultyGroup b)
            => !(a == b);

        public override int GetHashCode()
        {
            int hashCode = 1884815483;
            hashCode = hashCode * -1521134295 + Color.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + Precedence.GetHashCode();
            return hashCode;
        }
    }
}