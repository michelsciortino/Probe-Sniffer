using MongoDB.Bson;
using System;

namespace Core.Models.Database
{
    public class IntervalDataEntry
    {
        public ObjectId Id { get; set; }
        public int IntervalId { get; set; }
        public Device Sender { get; set; }
        public string SSID { get; set; }
        public DateTime Timestamp { get; set; }
        public string Hash { get; set; }
    }
}
