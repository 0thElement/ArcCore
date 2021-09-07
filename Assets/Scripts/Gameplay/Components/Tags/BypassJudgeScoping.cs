using Unity.Entities;

namespace ArcCore.Gameplay.Components.Tags
{
    //Mainly for decorative elements like beatlines, height indicator, archead,... where no judgement is performed
    //and scoping it further doesn't justify the cost of moving entities around chunks
    [GenerateAuthoringComponent]
    public struct BypassJudgeScoping : IComponentData 
    {}
}