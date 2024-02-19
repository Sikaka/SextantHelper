using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SextantHelper.Data
{

    public class TftItemList
    {
        public long timestamp { get; set; }
        public TftItemDetails[] data { get; set; }
    }
}
