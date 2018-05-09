using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace Core.Models.Database
{
    public class IntervalDescriptionEntry
    {
        public ObjectId Id { get; set; }
        public int IntervalId { get; set; }
        public List<Device> ActiveEsps { get; set; }
        public int NumberOfDetectedDevices { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
