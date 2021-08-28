namespace ArcCore.Serialization.NewtonsoftExtensions
{
    [JsonHasPresets]
    public interface IJsonPresetSpecialization<T>
    {
        public T Value { get; set; }
    }
}