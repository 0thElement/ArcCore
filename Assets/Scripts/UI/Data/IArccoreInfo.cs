using System.Collections.Generic;

namespace ArcCore.UI.Data
{
    public interface IArccoreInfo
    {
        IEnumerable<string> GetReferences();
        IList<string> ImportedGlobals { get; set; }
    }
}
