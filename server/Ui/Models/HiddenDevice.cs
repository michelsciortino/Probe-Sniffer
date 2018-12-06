using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ui.Models
{
    public class HiddenDevice
    {
        public List<string> SsidList { get; set; }
        public List<string> MacList { get; set; }
        public int Id { get; set; }
    }
}
