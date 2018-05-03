using Core.Models;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Core.DeviceCommunication.Messages;
using System.Diagnostics;
using System;

namespace Core.DeviceCommunication
{
    public static class DeviceCommunication
    {
        public const int SERVER_PORT = 45445;
        public const int DEVICE_PORT = 45448;

        public static bool Initialize(IList<ESP32_Device> devices) {
            bool result;
            CancellationTokenSource cancellation = new CancellationTokenSource();
            Thread ServerAdvertismentThread = new Thread(() => { DoAdvertisement(cancellation.Token); });
            ServerAdvertismentThread.Start();
            result=ReceiveDeviceReadys(devices);
            if(result)
                foreach (Device d in devices)
                {
                    result = SendPacketToDevice(new Ok_Message().ToString(), d);
                    if (result == false)
                        break;
                }
            try
            {
                cancellation.Cancel();
                while (ServerAdvertismentThread.IsAlive) Thread.Sleep(10);
                ServerAdvertismentThread.Abort();
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return result;
        }


        public static void DoAdvertisement(CancellationToken token)
        {
            Server_Advertisement_Message advertisement = new Server_Advertisement_Message();
            IPAddress broadcast = LocalNetworkConnection.GetBroadcastAddress();

            while (true)
            {
                if (token.IsCancellationRequested) break;
                UDPsender.Send(broadcast, SERVER_PORT, advertisement.ToString());
                Thread.Sleep(1000);
            }
        }

        public static bool ReceiveDeviceReadys(IList<ESP32_Device> devices)
        {
            int received = 0;
            string mac = "";
            IPAddress ip = null;
            bool result = false;
            

            while (received!=devices.Count)
            {
                
                result = ESPManager.ReceiveDeviceReady(ref mac, ref ip);
                if (result is true)
                    for (int i = 0; i < devices.Count; i++)
                        if (devices[i].MAC == mac)
                        {
                            devices[i].Ip = ip;
                            devices[i].Active = true;
                            received++;
                            break;
                        }
            }
            return false;
        }

        /// <summary>
        /// Sends a packet to a device
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="packet"></param>
        /// <returns></returns>
        private static bool SendPacketToDevice(string packet, Device device)
        {


            return true;
        }

    }

    

}
