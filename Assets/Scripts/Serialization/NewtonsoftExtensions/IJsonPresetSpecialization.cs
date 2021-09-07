namespace ArcCore.Serialization.NewtonsoftExtensions
{
    [JsonHasPresets]
    public interface IJsonPresetSpecialization<T>
    {
        T Value { get; set; }
    }
}