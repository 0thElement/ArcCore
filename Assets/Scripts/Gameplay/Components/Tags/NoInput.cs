using Unity.Entities;

namespace ArcCore.Gameplay.Components.Tags
{
    //Used to mark a note as noinput.
    //Noinput notes are decorative elements that does not count toward the score and can't be hit
    [GenerateAuthoringComponent]
    public struct NoInput : IComponentData 
    {}
}