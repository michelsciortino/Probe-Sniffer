using Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Core.DeviceCommunication
{
    public static class ESPManager
    {
        private static Dictionary<string, ESP32_Device> configuredESP = new Dictionary<string, ESP32_Device>();
        private static Mutex espMutex = new Mutex();

        public static void Initialize(List<Device> esps)
        {
            int n = esps.Count;
            for(int i =0;i<n;i++)
                configuredESP.Add(esps[i].MAC, new ESP32_Device(esps[i]));
        }

        public static List<ESP32_Device> ESPs => configuredESP.Values.ToList();

        public static void SetDeviceStatus(string mac,bool active)
        {
            espMutex.WaitOne();
            configuredESP[mac].Active = active;
            espMutex.ReleaseMutex();
        }

        public static ESP32_Device GetESPDevice(string mac)
        {
            bool result;
            espMutex.WaitOne();
            result = configuredESP.ContainsKey(mac);
            espMutex.ReleaseMutex();
            if (result is true)
                return configuredESP[mac];
            else
                return null;
        }

        public static bool IsESPConfigured(string mac)
        {
            bool result;
            espMutex.WaitOne();
            result = configuredESP.ContainsKey(mac);
            espMutex.ReleaseMutex();
            return result;
        }

        public static void WaitForMinESPsToConnect()
        {
            List<KeyValuePair<string, ESP32_Device>> result;
            while (true)
            {
                espMutex.WaitOne();
                result = configuredESP.Where(e => e.Value.Active).ToList();
                espMutex.ReleaseMutex();
                if (result.Count() < 2)
                    Thread.Sleep(1000);
                else
                    break;
            }
        }
    }
}
