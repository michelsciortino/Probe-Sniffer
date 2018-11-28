using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class HiddenDeviceInfo
    {
        public int NumProbes { get; set; }
        public List<String> SsidList { get; set; }
        public List<String> MacList { get; set; }
    }
}
