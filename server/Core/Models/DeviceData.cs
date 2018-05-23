using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Core.Models
{
    public class DeviceData
    {
        public string Esp_Mac { get; set; }
        public DateTime Timestamp { get; set; }
        public List<Packet> Packets { get; set; }
        

        public DeviceData()
        {
            Packets = new List<Packet>();
        }

        public static DeviceData FromJson(string json)
        {
            DeviceData deviceData = null;
            try
            {
                deviceData = JsonConvert.DeserializeObject<DeviceData>(json);
                return deviceData;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                //throw new Exception("Error deserializing data.", ex);
                return null;
            }
        }

        public string GetJson()
        {
            string json = "";
            try
            {
                json = JsonConvert.SerializeObject(this);
            }
            catch(Exception ex)
            {
                throw new Exception("Error serializing data.", ex);
            }
            return json;
        }
    }
}
