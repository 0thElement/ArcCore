namespace ArcCore.Serialization.NewtonsoftExtensions
{
    [System.Obsolete]
    [JsonHasPresets]
    public interface IJsonPresetSpecialization<T>
    {
        T Value { get; set; }
    }
}