using System;

namespace ArcCore.Structs
{
    public readonly struct float_xy_fixed_z : IEquatable<float_xy_fixed_z>
    {
        public readonly float x;
        public readonly float y;
        public readonly fixed_dec z;

        public float_xy_fixed_z(float x, float y, fixed_dec z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public bool Equals(float_xy_fixed_z other)
            => x == other.x &&
               y == other.y &&
               z == other.z;

        public override bool Equals(object obj)
        {
            return obj is float_xy_fixed_z o &&
                   x == o.x &&
                   y == o.y &&
                   z == o.z;
        }

        public override int GetHashCode()
        {
            int hashCode = 373119288;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            hashCode = hashCode * -1521134295 + z.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(float_xy_fixed_z left, float_xy_fixed_z right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(float_xy_fixed_z left, float_xy_fixed_z right)
        {
            return !(left == right);
        }

        public static float_xy_fixed_z operator +(float_xy_fixed_z left, float_xy_fixed_z right)
        {
            return new float_xy_fixed_z(left.x + right.x, left.y + right.y, left.z + right.z);
        }

        public static float_xy_fixed_z operator -(float_xy_fixed_z left, float_xy_fixed_z right)
        {
            return new float_xy_fixed_z(left.x - right.x, left.y - right.y, left.z - right.z);
        }

        public static float_xy_fixed_z operator *(float_xy_fixed_z left, float right)
        {
            return new float_xy_fixed_z(left.x * right, left.y * right, left.z * right);
        }

        public static float_xy_fixed_z operator /(float_xy_fixed_z left, float right)
        {
            return new float_xy_fixed_z(left.x / right, left.y / right, left.z / right);
        }
    }
}
