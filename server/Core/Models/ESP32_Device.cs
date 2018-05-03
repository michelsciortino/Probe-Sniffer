using System.Net;

namespace Core.Models
{
    public class ESP32_Device : Device
    {
        public IPAddress Ip { get; set; } = null;
        public bool Active { get; set; } = false;
    }
}
