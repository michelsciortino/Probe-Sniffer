using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class HiddenDeviceInfo
    {
        public int Id { get; set; }
        public HashSet<String> MacList { get; set; }
        public HashSet<String> SsidList { get; set; }
    }
}
