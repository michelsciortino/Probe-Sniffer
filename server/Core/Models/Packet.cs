using System;

namespace Core.Models
{
    public class Packet
    {
        public string MAC { get; set; }
        public string SSID { get; set; }
        public DateTime Timestamp { get; set; }
        public string Hash { get; set; }
        public int SignalStrength { get; set; }
    }
}
