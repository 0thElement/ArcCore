using System;

namespace ArcCore.Serialization.NewtonsoftExtensions
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct)]
    public class JsonHasPresetsAttribute : Attribute
    {}
}