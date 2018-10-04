using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Ui.Models
{
    public class DeviceStatistics
    {
        public string MAC { get; set; }
        public int Tot_Probes { get; set; }
        public bool Active { get; set; }
        public SolidColorBrush LineColor { get; set; }
        public int[] Probes { get; set; }
    }
}
