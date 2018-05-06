using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Core.Models
{
    public class DeviceData
    {
        public ICollection<Packet> Packets { get; set; }

        public DeviceData()
        {
            Packets = new List<Packet>();
        }

        public DeviceData(string json)
        {
            ICollection<Packet> data = null;
            try
            {
                data = JsonConvert.DeserializeObject<ICollection<Packet>>(json);
            }
            catch(Exception ex)
            {
                throw new Exception("Error deserializing data.", ex);
            }
            Packets = data;
        }

        public string GetJson()
        {
            string json = "";
            try
            {
                json = JsonConvert.SerializeObject(Packets);
            }
            catch(Exception ex)
            {
                throw new Exception("Error serializing data.", ex);
            }
            return json;
        }
    }
}
