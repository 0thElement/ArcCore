using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace ArcCore.UI.Data
{
    public class Pack : IArccoreInfo
    {
        public IList<string> ImportedGlobals { get; set; }
        public string ImagePath { get; set; }

        public string Name { get; set; }
        public string NameRomanized { get; set; }

        public IEnumerable<string> References
        {
            get
            {
                yield return ImagePath;
            }
        }

        public ulong Id { get; set; }

        public void ModifyReferences(Func<string, string> modifier)
        {
            ImagePath = modifier(ImagePath);
        }
    }
}