using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProbeSniffer.Models
{
    [Serializable]
    public class Device
    {
        public string MAC { get; set; }
        public int X_position { get; set; }
        public int Y_Position { get; set; }
    }
}
