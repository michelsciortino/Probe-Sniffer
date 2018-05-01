using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class DatabaseEntry
    {
        public ObjectId Id { get; set; }
        public int IntervalId { get; set; }
        public Device Device { get; set; }
        public string SSID { get; set; }
        public DateTime Timestamp { get; set; }
        public string Hash { get; set; }
    }
}
