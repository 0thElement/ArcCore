using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArcCore.Structs
{
    /// <summary>
    /// NOT ieee754 compliant... yet!
    /// </summary>
    public readonly struct FixedQ7 : ICloneable, IComparable, IComparable<FixedQ7>, IEquatable<FixedQ7>
    {
        //Integer representation
        private readonly long rawValue;

        #region Constants
        private const int SHF = 7 - 1; //Subtract one in order to account for one-indexed bits

        private const int ONE_REPR = 1 << SHF;
        private const float UNMUL = 1f / ONE_REPR;
        private const decimal UNMULDEC = (decimal)UNMUL;

        private const int PRESHF = SHF - 1;
        private const int DECMASK = 1 << SHF - 1;

        private const long HALF_REPR = 1 << PRESHF;
        #endregion

        //Direct construction, private because it is not intended to be called by any users
        private FixedQ7(long rawValue) => this.rawValue = rawValue;

        #region Factory methods
        public static FixedQ7 FromInt(int value) => new FixedQ7(value << SHF);

        /// <summary>
        /// <b>!!WARNING!!</b> Causes overflow if |value| > 2^(63 - Q number) - 1
        /// </summary>
        public static FixedQ7 FromLong(long value) => new FixedQ7(value << SHF);

        /// <summary>
        /// <b>!!WARNING!!</b> Causes overflow if |value| > 2^(63 - Q number) - 1
        /// </summary>
        public static FixedQ7 FromULong(ulong value) => new FixedQ7((long)(value << SHF));

        /// <summary>
        /// <b>!!WARNING!!</b> Causes overflow if |value| > 2^(63 - Q number) - 1
        /// </summary>
        public static FixedQ7 FromFloat(float value) => new FixedQ7((long)(value * ONE_REPR));

        /// <summary>
        /// <b>!!WARNING!!</b> Causes overflow if |value| > 2^(63 - Q number) - 1
        /// </summary>
        public static FixedQ7 FromDouble(double value) => new FixedQ7((long)(value * ONE_REPR));

        /// <summary>
        /// <b>!!WARNING!!</b> Causes overflow if |value| > 2^(63 - Q number) - 1
        /// </summary>
        public static FixedQ7 FromDecimal(decimal value) => new FixedQ7((long)(value * ONE_REPR));
        #endregion

        public long DecimalSection => rawValue & DECMASK;

        #region Math Operators
        public FixedQ7 Add(FixedQ7 other) => new FixedQ7(rawValue + other.rawValue);
        public static FixedQ7 operator +(FixedQ7 l, FixedQ7 r) => l.Add(r);

        public FixedQ7 Sub(FixedQ7 other) => new FixedQ7(rawValue - other.rawValue);
        public static FixedQ7 operator -(FixedQ7 l, FixedQ7 r) => l.Sub(r);

        public FixedQ7 Mul(FixedQ7 other) => new FixedQ7((long)(rawValue * other.rawValue * UNMUL));
        public static FixedQ7 operator *(FixedQ7 l, FixedQ7 r) => l.Mul(r);

        public FixedQ7 Div(FixedQ7 other) => new FixedQ7(rawValue / other.rawValue * ONE_REPR);
        public static FixedQ7 operator /(FixedQ7 l, FixedQ7 r) => l.Div(r);

        public FixedQ7 Abs() => new FixedQ7(rawValue < 0L ? -rawValue : rawValue);
        public FixedQ7 Floor() => new FixedQ7((rawValue >> SHF) << SHF); //hacky
        public FixedQ7 Ceil() => new FixedQ7(((rawValue >> SHF) + 1L) << SHF); //hacky
        public FixedQ7 Round()
        {

            long d = DecimalSection;
            if (d == HALF_REPR)
            {
                return rawValue < 0 ? Floor() : Ceil();
            }

            return d < HALF_REPR ? Floor() : Ceil();

        }

        public FixedQ7 Mod(FixedQ7 other) => new FixedQ7(rawValue - (other * (this / other).Floor()).rawValue);
        public static FixedQ7 operator %(FixedQ7 l, FixedQ7 r) => l.Mod(r);

        public FixedQ7 Neg() => new FixedQ7(-rawValue);
        public static FixedQ7 operator +(FixedQ7 v) => v; //Fuck you c#
        public static FixedQ7 operator -(FixedQ7 v) => v.Neg();
        #endregion

        #region Cast Operators
        public long AsLong() => rawValue >> SHF;
        public float AsFloat() => rawValue * UNMUL;
        public double AsDouble() => rawValue * UNMUL;
        public decimal AsDecimal() => rawValue * UNMULDEC;

        public static implicit operator FixedQ7(int i) => FromInt(i);
        public static explicit operator int(FixedQ7 f) => (int)f.AsLong();

        public static implicit operator FixedQ7(char c) => FromInt(c);
        public static explicit operator char(FixedQ7 f) => (char)f.AsLong();

        public static explicit operator FixedQ7(long l) => FromLong(l);
        public static explicit operator long(FixedQ7 f) => f.AsLong();

        public static explicit operator FixedQ7(ulong l) => FromULong(l);
        public static explicit operator ulong(FixedQ7 f) => (ulong)f.AsLong();

        public static explicit operator FixedQ7(float f) => FromFloat(f);
        public static explicit operator float(FixedQ7 f) => f.AsFloat();

        public static explicit operator FixedQ7(double d) => FromDouble(d);
        public static explicit operator double(FixedQ7 f) => f.AsDouble();

        public static explicit operator FixedQ7(decimal d) => FromDecimal(d);
        public static explicit operator decimal(FixedQ7 f) => f.AsDecimal();
        #endregion

        //CLONING
        public object Clone()
            => new FixedQ7(rawValue);

        //EQUALITY
        public override bool Equals(object obj)
            => obj is FixedQ7 f && this.Equals(f);
        public bool Equals(FixedQ7 other)
            => rawValue == other.rawValue;
        public override int GetHashCode()
            => rawValue.GetHashCode();

        //Comparisons
        public int CompareTo(object other)
        {
            if (other is FixedQ7 f)
                return CompareTo(f);
            throw new ArgumentException("Invalid type for comparison");
        }
        public int CompareTo(FixedQ7 other)
            => rawValue.CompareTo(other.rawValue);

        #region Relational Operators
        public static bool operator ==(FixedQ7 l, FixedQ7 r) => l.Equals(r);
        public static bool operator !=(FixedQ7 l, FixedQ7 r) => !l.Equals(r);
        public static bool operator >(FixedQ7 l, FixedQ7 r) => l.CompareTo(r) > 0;
        public static bool operator >=(FixedQ7 l, FixedQ7 r) => l.CompareTo(r) >= 0;
        public static bool operator <(FixedQ7 l, FixedQ7 r) => l.CompareTo(r) < 0;
        public static bool operator <=(FixedQ7 l, FixedQ7 r) => l.CompareTo(r) <= 0;
        #endregion

        public override string ToString()
        {
            // BUGGY, BUT BETTER APPROACH
            /*
            FixedQ7 n;
            FixedQ7 n2 = Abs();

            int nmod;

            string output = rawValue < 0 ? "-" : "";

            while (n2 != 0) 
            {
                n = n2;

                if (1 <= n && n2 < 1)
                {
                    output += '.';
                }

                nmod = (int)(n2 % 10);
                output += (char)('0' + nmod);

                n2 = n / 10 - nmod;
            }

            return output;
            */

            // SHORT TERM SOLUTION
            return ((decimal)this).ToString();

        }

    }
}
