public struct HoldFunnel
{
    public LongnoteVisualState visualState;
    public bool isHit;

    public HoldFunnel(LongnoteVisualState visualState, bool isHit)
    {
        this.visualState = visualState;
        this.isHit = isHit;
    }
}
