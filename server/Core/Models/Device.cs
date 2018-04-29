using System;

namespace Core.Models
{
    [Serializable]
    public class Device
    {
        public string MAC { get; set; }
        public int X_position { get; set; }
        public int Y_Position { get; set; }
    }
}
