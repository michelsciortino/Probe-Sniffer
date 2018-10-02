using System;

namespace Core.Models.Database
{
    public class Probe
    {
        public Device Sender { get; set; }
        public string SSID { get; set; }
        public DateTime Timestamp { get; set; }
        public string Hash { get; set; }
    }
}
