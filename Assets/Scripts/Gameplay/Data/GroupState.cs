namespace ArcCore.Gameplay.Data
{
    public enum GroupState
    {
        //Default state
        Initial,
        //Went past judge line but missed
        Missed,
        //Is being held
        Held,
        //Was held before
        Lifted
    }

}