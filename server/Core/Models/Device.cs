using System;

namespace Core.Models
{
    [Serializable]
    public class Device
    {
        public string MAC { get; set; }
        public double X_Position { get; set; }
        public double Y_Position { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
