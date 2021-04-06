using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArcCore.Structs
{
    /// <summary>
    /// NOT iiee754 compliant... yet!
    /// </summary>
    public readonly struct FixedQ7
    {
        //Integer representation
        private readonly long rawValue;

        //Shift values
        private const int SHF = 7;

        private const int MUL = 1 << SHF;
        private const float UNMUL = 1 / MUL;
        private const decimal UNMULDEC = (decimal)UNMUL;

        //Direct construction, private because it is not intended to be called by any users
        private FixedQ7(long rawValue) => this.rawValue = rawValue;

        #region Factory methods
        public static FixedQ7 FromInt(int value) => new FixedQ7(value << SHF);

        /// <summary>
        /// <b>!!WARNING!!</b> Causes overflow if |value| > 2^(64 - Q number) - 1
        /// </summary>
        public static FixedQ7 FromLong(long value) => new FixedQ7(value << SHF);

        /// <summary>
        /// <b>!!WARNING!!</b> Causes overflow if |value| > 2^(64 - Q number) - 1
        /// </summary>
        public static FixedQ7 FromULong(ulong value) => new FixedQ7((long)(value << SHF));

        /// <summary>
        /// <b>!!WARNING!!</b> Causes overflow if |value| > 2^(64 - Q number) - 1
        /// </summary>
        public static FixedQ7 FromFloat(float value) => new FixedQ7((long)(value * MUL));

        /// <summary>
        /// <b>!!WARNING!!</b> Causes overflow if |value| > 2^(64 - Q number) - 1
        /// </summary>
        public static FixedQ7 FromDouble(double value) => new FixedQ7((long)(value * MUL));

        /// <summary>
        /// <b>!!WARNING!!</b> Causes overflow if |value| > 2^(64 - Q number) - 1
        /// </summary>
        public static FixedQ7 FromDecimal(decimal value) => new FixedQ7((long)(value * MUL));

        #endregion

        #region Math Operators
        public FixedQ7 Add(FixedQ7 other) => new FixedQ7(rawValue + other.rawValue);
        public static FixedQ7 operator +(FixedQ7 l, FixedQ7 r) => l.Add(r);

        public FixedQ7 Sub(FixedQ7 other) => new FixedQ7(rawValue - other.rawValue);
        public static FixedQ7 operator -(FixedQ7 l, FixedQ7 r) => l.Sub(r);

        public FixedQ7 Mul(FixedQ7 other) => new FixedQ7((long)(rawValue * other.rawValue * UNMUL));
        public static FixedQ7 operator *(FixedQ7 l, FixedQ7 r) => l.Mul(r);

        public FixedQ7 Div(FixedQ7 other) => new FixedQ7(rawValue / other.rawValue * MUL);
        public static FixedQ7 operator /(FixedQ7 l, FixedQ7 r) => l.Div(r);

        public FixedQ7 Abs() => new FixedQ7(rawValue < 0 ? -rawValue : rawValue);
        public FixedQ7 Round() => new FixedQ7((rawValue >> SHF) << SHF);

        public FixedQ7 Mod(FixedQ7 other) => new FixedQ7(rawValue - other.Mul(Div(other).Round()).rawValue);
        public static FixedQ7 operator %(FixedQ7 l, FixedQ7 r) => l.Mod(r);

        public FixedQ7 Neg() => new FixedQ7(-rawValue);
        public static FixedQ7 operator +(FixedQ7 v) => v; //Fuck you c#
        public static FixedQ7 operator -(FixedQ7 v) => v.Neg();
        #endregion

        #region Cast Operators
        public static implicit operator FixedQ7(int i) => FromInt(i);
        public static explicit operator int(FixedQ7 f) => (int)(f.rawValue >> SHF);

        public static explicit operator FixedQ7(long l) => FromLong(l);
        public static explicit operator long(FixedQ7 f) => f.rawValue >> SHF;

        public static explicit operator FixedQ7(ulong l) => FromULong(l);
        public static explicit operator ulong(FixedQ7 f) => (ulong)(f.rawValue >> SHF);

        public static explicit operator FixedQ7(float f) => FromFloat(f);
        public static explicit operator float(FixedQ7 f) => f.rawValue * UNMUL;

        public static explicit operator FixedQ7(double d) => FromDouble(d);
        public static explicit operator double(FixedQ7 f) => f.rawValue * UNMUL;

        public static explicit operator FixedQ7(decimal d) => FromDecimal(d);
        public static explicit operator decimal(FixedQ7 f) => f.rawValue * UNMULDEC;
        #endregion

    }
}
