namespace ArcCore.Data
{
    public class CutoffUtils
    {
        public static readonly ShaderCutoff Unjudged = new ShaderCutoff() { Value = 1f };
        public static readonly HitState UnjudgedHS   = new HitState() { Value = 1f, HitRaw = false };
        public static readonly ShaderCutoff JudgedP  = new ShaderCutoff() { Value = 0f };
        public static readonly HitState JudgedPHS    = new HitState() { Value = 0f };
        public static readonly ShaderCutoff JudgedL  = new ShaderCutoff() { Value = 2f };
        public static readonly HitState JudgedLHS    = new HitState() { Value = 2f };
    }
}
