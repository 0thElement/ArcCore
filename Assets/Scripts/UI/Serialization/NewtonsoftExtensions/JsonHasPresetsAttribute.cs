using System;

namespace ArcCore.Serialization.NewtonsoftExtensions
{
    [System.Obsolete]
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct)]
    public class JsonHasPresetsAttribute : Attribute
    {}
}