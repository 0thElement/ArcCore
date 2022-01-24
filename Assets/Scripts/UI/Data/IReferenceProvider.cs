using System.Collections.Generic;

namespace ArcCore.UI.Data
{
    public interface IReferenceProvider
    {
        IEnumerable<string> GetReferences();
    }
}
