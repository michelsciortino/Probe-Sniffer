using MongoDB.Bson;
using System;

namespace Core.Models
{
    public class Packet
    {
        public ObjectId _id { get; set; }
        public string MAC { get; set; }
        public string SSID { get; set; }
        public DateTime Timestamp { get; set; }
        public string Hash { get; set; }
        public int SignalStrength { get; set; }
        public string ESP_MAC { get; set; }
    }
}
