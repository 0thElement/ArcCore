using System;
using System.Collections.Generic;

namespace ArcCore.Serialization
{
    public class DifficultyType : ICloneable, IEquatable<DifficultyType>
    {
        public string fullName;
        public string idName;

        public float sortOrder;

        public Color textColor;
        public Color baseColor;

        public object Clone() => new DifficultyType
        {
            fullName = fullName,
            idName = idName,
            sortOrder = sortOrder,
            textColor = textColor,
            baseColor = baseColor
        };

        public bool Equals(DifficultyType other)
        {
            return fullName == other.fullName
                && idName == other.idName
                && sortOrder == other.sortOrder
                && textColor == other.textColor
                && baseColor == other.baseColor;
        }

        public override bool Equals(object obj)
            => obj is DifficultyType diffType && Equals(diffType);

        public static bool operator ==(DifficultyType a, DifficultyType b)
            => a.Equals(b);
        public static bool operator !=(DifficultyType a, DifficultyType b)
            => !(a == b);

        #region Standard Difficulty Classes
        public static DifficultyType Past => new DifficultyType
        {
            fullName = "Past",
            idName = "pst",

            sortOrder = 0f,

            textColor = Color.FromHexcode("#FFFFFF"),
            baseColor = Color.FromHexcode("#B0FFA0")
        };
        public static DifficultyType Present => new DifficultyType
        {
            fullName = "Present",
            idName = "prs",

            sortOrder = 1f,

            textColor = Color.FromHexcode("#FFFFFF"),
            baseColor = Color.FromHexcode("#0020FF")
        };
        public static DifficultyType Future => new DifficultyType
        {
            fullName = "Future",
            idName = "ftr",

            sortOrder = 1f,

            textColor = Color.FromHexcode("#FFFFFF"),
            baseColor = Color.FromHexcode("#FF2000")
        };
        public static DifficultyType beyond = new DifficultyType
        {
            fullName = "Future",
            idName = "ftr",

            sortOrder = 1f,

            textColor = Color.FromHexcode("#FFFFFF"),
            baseColor = Color.FromHexcode("#D00000")
        };
        #endregion
        public static Dictionary<string, DifficultyType> Presets
            => new Dictionary<string, DifficultyType>
            {
            {"past", Past},
            {"present", Present},
            {"future", Future}
            };
    }
}