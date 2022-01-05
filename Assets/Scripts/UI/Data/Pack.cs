using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace ArcCore.UI.Data
{
    public class Pack : IArccoreInfo
    {
        public IList<string> ImportedGlobals { get; set; }

        public Level[] Levels { get; set; }

        public IEnumerable<string> GetReferences()
            => Levels.SelectMany(c => c.GetReferences());
    }
}