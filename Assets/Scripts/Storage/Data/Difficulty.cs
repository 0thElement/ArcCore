using System.Collections.Generic;
using System.Linq;

namespace ArcCore.Storage.Data
{
    public class Difficulty
    {
        public string Name { get; set; }
        public bool IsPlus { get; set; }

        public Difficulty(string name)
        {
            IsPlus = false;

            if (name.EndsWith("+") && name.Length > 1)
            {
                Name = name.Substring(0, name.Length - 1);
                IsPlus = true;
            }
            Name = name;
        }
    }
}