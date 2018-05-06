using Core.Models;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System;
using Core.DeviceCommunication.Messages.Server_Messages;
using Core.DeviceCommunication.Messages.ESP32_Messages;

namespace Core.DeviceCommunication
{
    public class DeviceCommunication
    {
        #region Const
        public const int SERVER_PORT = 45445;
        public const int DEVICE_PORT = 45448;
        private const int INIT_TIMEOUT = 30000;
        #endregion

        #region Private Properties
        private bool _initialized = false;
        private List<ESP32_Device> esp32s = null;
        private ESPManager espManager = null;
        #endregion

        #region Public Properties
        public bool Initialized { get => _initialized; }
        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the Device Communication by signaling the Server IP via UDP broadcast and receiving the Device Ready message from all the connected ESP32 <paramref name="devices"/>
        /// </summary>
        /// <param name="devices">List of ESP32 devices</param>
        /// <returns>True if the communication has been initialized correctly, False otherwise</returns>
        public bool Initialize(List<Device> devices)
        {
            esp32s = new List<ESP32_Device>();
            foreach (Device e in devices)
                esp32s.Add(new ESP32_Device(e));

            espManager = new ESPManager(LocalNetworkConnection.GetLocalIp(), DeviceCommunication.SERVER_PORT);
            espManager.Timeout = 10; // 10 seconds
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
            result = ReceiveDeviceReadys(INIT_TIMEOUT);
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
        #endregion

        #region Static Methods

        /// <summary>
        /// Sends the Server Advertisement message broadcast via UDP
        /// </summary>
        /// <param name="token">Cancellation token to stop the broadcasting</param>
        public static void DoAdvertisement(CancellationToken token)
        {
            Server_Advertisement_Message advertisement = new Server_Advertisement_Message();
            IPAddress broadcast = LocalNetworkConnection.GetBroadcastAddress();

            while (true)
            {
                if (token.IsCancellationRequested) break;
                UdpBroadcaster.Send(broadcast, SERVER_PORT, advertisement.ToBytes());
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Tries to receive the Device Ready message from all the ESP32 devices in the list in <paramref name="timeout"/> time (milliseconds)
        /// </summary>
        /// <param name="devices">>List of ESP32 devices</param>
        /// <param name="timeout">The timeout</param>
        /// <returns>True if all the devices sent the Device Ready message, False otherwise</returns>
        /// <remarks>The method set the <paramref name="Ip"/> address and the <paramref name="Status"/> of each ESP32 device which responded </remarks>
        public bool ReceiveDeviceReadys(int timeout)
        {
            int received = 0;
            bool timedOut = false;
            Timer timer = new Timer((_) => { timedOut = true; }, null, timeout, Timeout.Infinite);
            while (received != esp32s.Count)
            {
                if (timedOut) break;
                Ready_Message ready = espManager.ReceiveReadyMessage();
                if (ready != null)
                    for (int i = 0; i < esp32s.Count; i++)
                        if (esp32s[i].MAC == ready.Payload)
                        {
                            esp32s[i].Ip = ready.EspIPAddress;
                            esp32s[i].Active = true;
                            received++;
                            break;
                        }
            }
            if (received == esp32s.Count)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Sends a message to a ESP3 device
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="device">The ESP32 device</param>
        /// <returns>True if the message has been sent correctly, False otherwise</returns>
        private static bool SendPacketToDevice(byte[] message, ESP32_Device device)
        {
            //TO BE IMPLEMENTED
            return true;
        }

        #endregion

    }
}