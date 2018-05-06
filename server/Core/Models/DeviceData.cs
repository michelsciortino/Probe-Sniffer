using System.Collections.Generic;

namespace Core.Models
{
    public class DeviceData
    {
        public ICollection<Packet> Packets { get; set; }
    }
}
