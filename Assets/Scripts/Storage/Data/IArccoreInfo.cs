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
        int Id { get; set; }
        string ExternalId { get; set; }
        List<string> FileReferences { get; set; }
        int Insert();
        void Delete();
        int Update(IArccoreInfo other);
        List<IArccoreInfo> ConflictingExternalIdentifier();
    }
}