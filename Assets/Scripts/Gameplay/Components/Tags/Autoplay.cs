using Unity.Entities;

namespace ArcCore.Gameplay.Components.Tags
{
    //Used to mark a note as autoplay
    //Can mark every note with autoplay or a number of note groups only
    [GenerateAuthoringComponent]
    public struct Autoplay : IComponentData 
    {}
}