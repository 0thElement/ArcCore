using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArcCore.Serialization
{
    public readonly struct LevelMetadata
    {
        //Song information
        public readonly string title;
        public readonly string artist;
        public readonly string bpmData;

        //Illustration information
        public readonly string illustrator;
        public readonly Dictionary<int, string> imageOverrides;

        //Chart information
        public readonly string charter;
        public readonly float? baseBpmOverride;

        //Chart difficulties
        public readonly Difficulty[] difficulties;
    }

    public readonly struct Difficulty
    {

    }
}
