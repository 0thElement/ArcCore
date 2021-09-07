using System;

namespace ArcCore.Serialization.NewtonsoftExtensions
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class JsonPresetAttribute : Attribute
    {
        public string NameOverride { get; set; }
    }
}