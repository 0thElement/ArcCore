namespace ArcCore.Gameplay.Parsing
{
    public class AffError
    {
        public AffErrorType type;
        public int line;

        public AffError(AffErrorType type, int line)
        {
            this.type = type;
            this.line = line;
        }

        public override string ToString() => $"{type} @ line {line}";
    }
}
