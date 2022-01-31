using System.Collections.Generic;

namespace ArcCore.Storage.Data
{
    public interface IArccoreInfo
    {
        ///<summary>
        ///Check assets for missing references, and remove unused assets
        ///</summary>
        List<string> TryApplyReferences(List<string> availableAssets, out string missing);
        string VirtualPathPrefix();
        string ExternalIdentifier();
        void Insert();
        void Delete();
        void Update(IArccoreInfo other);
        List<IArccoreInfo> ConflictingExternalIdentifier();
    }
}