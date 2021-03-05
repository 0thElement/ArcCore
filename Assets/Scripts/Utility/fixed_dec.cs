using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArcCore.Structs
{
    public readonly struct fixed_dec : IEquatable<fixed_dec>, IComparable<fixed_dec>
    {
        private readonly long repr;

        private fixed_dec(long repr) => this.repr = repr;

        public static readonly fixed_dec MinValue = new fixed_dec(long.MinValue);
        public static readonly fixed_dec MaxValue = new fixed_dec(long.MaxValue);

        //INTERFACE METHODS
        public bool Equals(fixed_dec other)
            => repr == other.repr;

        public override bool Equals(object obj)
            => obj is fixed_dec dec &&
               Equals(dec);

        public int CompareTo(fixed_dec other)
            => (int)(repr - other.repr);

        public override int GetHashCode() => -1319451962 + repr.GetHashCode();

        //MATH
        public fixed_dec Add(fixed_dec other)
            => new fixed_dec(unchecked(repr + other.repr));
        public fixed_dec Sub(fixed_dec other)
            => new fixed_dec(unchecked(repr - other.repr));
        public fixed_dec Mul(fixed_dec other)
            => new fixed_dec(unchecked(repr * other.repr));
        public fixed_dec Div(fixed_dec other)
            => new fixed_dec(unchecked(repr / other.repr));
        public fixed_dec Mod(fixed_dec other)
            => new fixed_dec(unchecked(repr % other.repr));

        //COMPARISONS
        public static bool operator ==(fixed_dec left, fixed_dec right)
            => left.Equals(right);
        public static bool operator !=(fixed_dec left, fixed_dec right)
            => !(left == right);
        public static bool operator >(fixed_dec left, fixed_dec right)
            => left.CompareTo(right) > 0;
        public static bool operator <(fixed_dec left, fixed_dec right)
            => left.CompareTo(right) < 0;
        public static bool operator >=(fixed_dec left, fixed_dec right)
            => left.CompareTo(right) >= 0;
        public static bool operator <=(fixed_dec left, fixed_dec right)
            => left.CompareTo(right) <= 0;

        //CONVERSIONS
        public static implicit operator fixed_dec(int v) 
            => new fixed_dec(v * 100);
        public static implicit operator fixed_dec(long v)
            => new fixed_dec(v * 100);
        public static explicit operator fixed_dec(float v)
            => new fixed_dec((long)(v * 100));
        public static explicit operator fixed_dec(double v)
            => new fixed_dec((long)(v * 100));
        public static explicit operator fixed_dec(decimal v)
            => new fixed_dec((long)(v * 100));

        public static explicit operator int(fixed_dec v)
            => (int)(v.repr / 100);
        public static explicit operator long(fixed_dec v)
            => v.repr / 100;
        public static implicit operator float(fixed_dec v)
            => v.repr / 100f;
        public static implicit operator double(fixed_dec v)
            => v.repr / 100d;
        public static implicit operator decimal(fixed_dec v)
            => v.repr / 100m;

        //MATH OPERATORS
        public static fixed_dec operator +(fixed_dec left, fixed_dec right)
            => left.Add(right);
        public static fixed_dec operator +(fixed_dec left, int right)
            => left.Add(right);
        public static fixed_dec operator +(fixed_dec left, long right)
            => left.Add(right);
        public static fixed_dec operator +(fixed_dec left, float right)
            => left.Add((fixed_dec)right);
        public static fixed_dec operator +(fixed_dec left, double right)
            => left.Add((fixed_dec)right);
        public static fixed_dec operator +(fixed_dec left, decimal right)
            => left.Add((fixed_dec)right);

        public static fixed_dec operator -(fixed_dec left, fixed_dec right)
            => left.Sub(right);
        public static fixed_dec operator -(fixed_dec left, int right)
            => left.Sub(right);
        public static fixed_dec operator -(fixed_dec left, long right)
            => left.Sub(right);
        public static fixed_dec operator -(fixed_dec left, float right)
            => left.Sub((fixed_dec)right);
        public static fixed_dec operator -(fixed_dec left, double right)
            => left.Sub((fixed_dec)right);
        public static fixed_dec operator -(fixed_dec left, decimal right)
            => left.Sub((fixed_dec)right);

        public static fixed_dec operator *(fixed_dec left, fixed_dec right)
            => left.Mul(right);
        public static fixed_dec operator *(fixed_dec left, int right)
            => left.Mul(right);
        public static fixed_dec operator *(fixed_dec left, long right)
            => left.Mul(right);
        public static fixed_dec operator *(fixed_dec left, float right)
            => left.Mul((fixed_dec)right);
        public static fixed_dec operator *(fixed_dec left, double right)
            => left.Mul((fixed_dec)right);
        public static fixed_dec operator *(fixed_dec left, decimal right)
            => left.Mul((fixed_dec)right);

        public static fixed_dec operator /(fixed_dec left, fixed_dec right)
            => left.Div(right);
        public static fixed_dec operator /(fixed_dec left, int right)
            => left.Div(right);
        public static fixed_dec operator /(fixed_dec left, long right)
            => left.Div(right);
        public static fixed_dec operator /(fixed_dec left, float right)
            => left.Div((fixed_dec)right);
        public static fixed_dec operator /(fixed_dec left, double right)
            => left.Div((fixed_dec)right);
        public static fixed_dec operator /(fixed_dec left, decimal right)
            => left.Div((fixed_dec)right);

        public static fixed_dec operator %(fixed_dec left, fixed_dec right)
            => left.Mod(right);
        public static fixed_dec operator %(fixed_dec left, int right)
            => left.Mod(right);
        public static fixed_dec operator %(fixed_dec left, long right)
            => left.Mod(right);
        public static fixed_dec operator %(fixed_dec left, float right)
            => left.Mod((fixed_dec)right);
        public static fixed_dec operator %(fixed_dec left, double right)
            => left.Mod((fixed_dec)right);
        public static fixed_dec operator %(fixed_dec left, decimal right)
            => left.Mod((fixed_dec)right);

        public static fixed_dec operator +(fixed_dec v)
            => v;
        public static fixed_dec operator -(fixed_dec v)
            => new fixed_dec(-v.repr);

    }
}
