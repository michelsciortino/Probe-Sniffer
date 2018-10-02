using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Core.Models.Database
{
    public class ProbesInterval
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public int IntervalId { get; set; }
        public List<ESP32_Device> ActiveEsps { get; set; }
        public List<Probe> Probes { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
