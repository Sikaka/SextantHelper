using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SextantHelper.Data
{
    public class TftItemDetails
    {
        public string name { get; set; }
        public double divine { get; set; }
        public double chaos { get; set; }
        public bool lowConfidence { get; set; }
        public double ratio { get; set; }
    }
}
