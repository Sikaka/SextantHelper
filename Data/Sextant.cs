using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SextantHelper.Data
{
    public class Sextant
    {
        public string Name { get; set; }
        public List<string> Mods { get; set; } = new List<string>();
        public Dictionary<string, int> Weights { get; set; } = new Dictionary<string, int>();

        public string Mods_Flattened { get { return string.Join(' ', Mods); } }

        public int Weights_Total { get { return Weights.Values.Sum(); } }

        public int MatchCount { get; set; }
        public double Price { get; set; }
    }
}
