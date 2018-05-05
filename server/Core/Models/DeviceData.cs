using System.Collections.Generic;

namespace Core.Models
{
    public class DeviceData
    {
        public ICollection<Packet> packets { get; set; }
    }
}
