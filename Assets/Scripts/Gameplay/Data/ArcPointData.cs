namespace ArcCore.Gameplay.Data
{
    /// <summary>
    /// Stores data requried to handle arc endpoints.
    /// </summary>
    public struct ArcPointData
    {
        public int timingGroup;
        public int time;
        public float x;
        public float y;
        public int color;

        public ArcPointData(int timingGruop, int time, float x, float y, int color)
        {
            this.timingGroup = timingGruop;
            this.time = time;
            this.x = x;
            this.y = y;
            this.color = color; 
        }

        public override bool Equals(object obj)
        {
            return obj is ArcPointData other &&
                    timingGroup == other.timingGroup &&
                    time == other.time &&
                    x == other.x &&
                    y == other.y &&
                    color == other.color;
        }

        public static bool operator ==(ArcPointData l, ArcPointData r) => l.Equals(r);
        public static bool operator !=(ArcPointData l, ArcPointData r) => !(l == r);

        public override int GetHashCode()
        {
            int hashCode = 1052165582;
            hashCode = hashCode * -1521134295 + timingGroup.GetHashCode();
            hashCode = hashCode * -1521134295 + time.GetHashCode();
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            hashCode = hashCode * -1521134295 + color.GetHashCode();
            return hashCode;
        }

        public void Deconstruct(out int timingGroup, out int time, out float x, out float y, out float color)
        {
            timingGroup = this.timingGroup;
            time = this.time;
            x = this.x;
            y = this.y;
            color = this.color;
        }

        public static implicit operator (int, int time, float, float, int)(ArcPointData value)
        {
            return (value.timingGroup, value.time, value.x, value.y, value.color);
        }

        public static implicit operator ArcPointData((int, int time, float, float, int) value)
        {
            return new ArcPointData(value.Item1, value.time, value.Item3, value.Item4, value.Item5);
        }
    }
}