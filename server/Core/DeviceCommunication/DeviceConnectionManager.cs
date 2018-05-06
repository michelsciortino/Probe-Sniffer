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
    public class DeviceConnectionManager
    {
        #region Const
        public const int SERVER_PORT = 45445;
        public const int DEVICE_PORT = 45448;
        private const int INIT_TIMEOUT = 30000;
        #endregion

        #region Private Properties
        private bool _initialized = false;
        private List<ESP32_Device> _esp32s = null;
        private ESPManager _espManager = null;
        #endregion

        #region Public Properties
        public bool Initialized => _initialized;
        public List<ESP32_Device> Devices => _esp32s;
        #endregion

        #region Private Methods
        /// <summary>
        /// Tries to receive the Device Ready message from all the ESP32 devices in the list in <paramref name="timeout"/> time (milliseconds)
        /// </summary>
        /// <param name="devices">>List of ESP32 devices</param>
        /// <param name="timeout">The timeout</param>
        /// <returns>True if all the devices sent the Device Ready message, False otherwise</returns>
        /// <remarks>The method set the <paramref name="Ip"/> address and the <paramref name="Status"/> of each ESP32 device which responded </remarks>
        private bool ReceiveDeviceReadys(int timeout)
        {
            int received = 0;
            bool timedOut = false;
            Timer timer = new Timer((_) => { timedOut = true; }, null, timeout, Timeout.Infinite);
            while (received != _esp32s.Count)
            {
                if (timedOut) break;
                Ready_Message ready = _espManager.ReceiveReadyMessage();
                if (ready != null)
                    for (int i = 0; i < _esp32s.Count; i++)
                        if (_esp32s[i].MAC == ready.Payload)
                        {
                            _esp32s[i].Ip = ready.EspIPAddress;
                            _esp32s[i].Active = true;
                            received++;
                            break;
                        }
            }
            if (received == _esp32s.Count)
                return true;
            else
                return false;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initializes the Device Communication by signaling the Server IP via UDP broadcast and receiving the Device Ready message from all the connected ESP32 <paramref name="devices"/>
        /// </summary>
        /// <param name="devices">List of ESP32 devices</param>
        /// <returns>True if the communication has been initialized correctly, False otherwise</returns>
        public bool Initialize(List<Device> devices)
        {
            bool result;
            _esp32s = new List<ESP32_Device>();
            foreach (Device e in devices)
                _esp32s.Add(new ESP32_Device(e));

            _espManager = new ESPManager(LocalNetworkConnection.GetLocalIp(), SERVER_PORT);
            _espManager.Timeout = 3; // 3 seconds
            
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

            //Sending Ok Message to each ESP32 device
            if (result is true)
            {
                int tries;
                foreach (ESP32_Device esp in _esp32s)
                {
                    tries = 0;
                    while (tries<3)
                    {
                        result = _espManager.SendOkMessage(esp, DEVICE_PORT);
                        if (result == true)
                            break;
                        tries++;
                    }
                }
            }
            try
            {
                cancellation.Cancel();
                while (ServerAdvertismentThread.IsAlive)
                    Thread.Sleep(10);
                ServerAdvertismentThread.Abort();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            _initialized = result;
            return result;
        }

        /// <summary>
        /// Sends the Server Advertisement message broadcast via UDP
        /// </summary>
        /// <param name="token">Cancellation token to stop the broadcasting</param>
        public void DoAdvertisement(CancellationToken token)
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
        /// Sends a Timestamp Message to all the ESP32 devices
        /// </summary>
        /// <returns></returns>
        public bool SendTimestampsToDevices()
        {
            bool result=false;
            int sent = 0,tries;

            foreach(ESP32_Device esp in _esp32s)
            {
                tries = 0;
                while (tries < 3)
                {
                    result=_espManager.SendTimestampMessage(esp, DEVICE_PORT);
                    if (result is false)
                        tries++;
                    else
                    {
                        sent++;
                        break;
                    }
                }
                if (result is false)
                    esp.Active = false;
            }
            if (sent == _esp32s.Count)
                result = true;
            else
                result = false;

            return result;
        }
        public List<DeviceData> CollectDevicesData()
        {
            List<DeviceData> devicesData = new List<DeviceData>();
            Data_Message message;
            DeviceData data = null;
            int tries;

            //Expecting _esp32s.Count DeviceData
            for (int i = 0; i < _esp32s.Count; i++)
            {
                tries = 0;
                //Trying 3 time to get a device data
                while (tries < 3)
                {
                    message = _espManager.ReceiveDataMessage();
                    if (message is null)
                        tries++;
                    else
                    {
                        data = DeviceData.FromJson(message.Payload);
                        if (data != null)
                            devicesData.Add(data);
                        break;
                    }
                }
            }

            return devicesData;
        }

        #endregion

    }
}