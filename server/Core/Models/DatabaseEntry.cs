using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace Core.Models
{
    public class DatabaseEntry
    {
        public ObjectId Id { get; set; }
        public int IntervalId { get; set; }
        public List<Device> ActiveESP32s { get; set; }
        public Device Device { get; set; }
        public string SSID { get; set; }
        public DateTime Timestamp { get; set; }
        public string Hash { get; set; }
    }
}
