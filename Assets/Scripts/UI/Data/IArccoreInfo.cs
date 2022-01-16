using System;
using System.Collections.Generic;

namespace ArcCore.UI.Data
{
    public interface IArccoreInfo
    {
        IEnumerable<string> References { get; }
        void ModifyReferences(Func<string, string> modifier);

        ulong Id { get; set; }

        IList<string> ImportedGlobals { get; set; }
    }

    public static class ArccoreInfoExtensions
    {
        public static ArccoreInfoType Type(this IArccoreInfo info)
        {
            switch(info)
            {
                case Level:
                    return ArccoreInfoType.Level;
                case Pack:
                    return ArccoreInfoType.Pack;
            }

            throw new NotImplementedException();
        }

        public static ArccoreInfoType Type<T>() where T : IArccoreInfo
        {
            if(typeof(T) == typeof(Level))
                return ArccoreInfoType.Level;
            if(typeof(T) == typeof(Pack))
                return ArccoreInfoType.Pack;

            throw new NotImplementedException();
        }
    }
}
