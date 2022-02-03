namespace ArcCore.Gameplay.Parsing.Data
{
    public class ChartError
    {
        public ChartErrorType type;
        public int line;

        public ChartError(ChartErrorType type, int line)
        {
            this.type = type;
            this.line = line;
        }

        public override string ToString() => $"{type} @ line {line}";
    }
}
