using System.Net;

namespace Core.Models
{
    public class ESP32_Device : Device
    {
        public IPAddress Ip { get; set; }
        public bool Active { get; set; }

        public ESP32_Device()
        {
            Ip = null;
            Active = false;
        }
        public ESP32_Device(Device device)
        {
            MAC = device.MAC;
            X_position = device.X_position;
            Y_Position = device.Y_Position;
            Ip = null;
            Active = false;
        }
    }
}
