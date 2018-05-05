using Core.Models;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System;
using Core.DeviceCommunication.Messages.Server_Messages;

namespace Core.DeviceCommunication
{
    public class DeviceCommunication
    {
        public const int SERVER_PORT = 45445;
        public const int DEVICE_PORT = 45448;

        private const int INIT_TIMEOUT = 30000;

        private bool _initialized = false;
        private List<ESP32_Device> esp32s = null;

        public bool Initialized { get => _initialized; }

        public bool Initialize(List<Device> devices)
        {
            esp32s = new List<ESP32_Device>();
            foreach (Device e in devices)
                esp32s.Add(new ESP32_Device(e));

            bool result;
            CancellationTokenSource cancellation = new CancellationTokenSource();
            Thread ServerAdvertismentThread = new Thread(() => { DoAdvertisement(cancellation.Token); });
            try
            {
                ServerAdvertismentThread.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _initialized = false;
                return false;
            }
            result = ReceiveDeviceReadys(esp32s);
            if (result)
                foreach (ESP32_Device esp in esp32s)
                {
                    result = SendPacketToDevice(new Ok_Message().ToBytes(), esp);
                    if (result == false)
                        break;
                }
            try
            {
                cancellation.Cancel();
                while (ServerAdvertismentThread.IsAlive) Thread.Sleep(10);
                ServerAdvertismentThread.Abort();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            _initialized = result;
            return result;
        }


        public static void DoAdvertisement(CancellationToken token)
        {
            Server_Advertisement_Message advertisement = new Server_Advertisement_Message();
            IPAddress broadcast = LocalNetworkConnection.GetBroadcastAddress();

            while (true)
            {
                if (token.IsCancellationRequested) break;
                UDPsender.Send(broadcast, SERVER_PORT, advertisement.ToBytes());
                Thread.Sleep(1000);
            }
        }

        public static bool ReceiveDeviceReadys(IList<ESP32_Device> devices)
        {
            int received = 0;
            string mac = "";
            IPAddress ip = null;
            bool result = false, timedOut = false;
            Timer timer = new Timer((_) => { timedOut = true; }, null, INIT_TIMEOUT, Timeout.Infinite);
            while (received != devices.Count)
            {
                if (timedOut) break;
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
        private static bool SendPacketToDevice(byte[] packet, ESP32_Device device)
        {


            return true;
        }

    }
}