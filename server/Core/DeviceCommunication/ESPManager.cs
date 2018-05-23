using Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Core.DeviceCommunication
{
    #region Old ESPManager
    /*
    public class ESPManager
    {
        #region Private Properties
        private IPAddress _localIpAddress = null;
        private int _receivingPort = -1;
        private int _timeout = -1;
        #endregion

        #region Constructor
        public ESPManager(IPAddress localIpAddress, int receivingPort)
        {
            _localIpAddress = localIpAddress;
            _receivingPort = receivingPort;
        }
        #endregion

        #region Public Properties
        public int Timeout { get => _timeout; set => _timeout = value; }
        #endregion

        #region Public Methods

        #region Messages Receiving
        
        /// <summary>
        /// Tries to receive a Ready Message from a device
        /// </summary>
        /// <returns>A Ready Message or null</returns>
        public Ready_Message ReceiveReadyMessage()
        {
            ESP_Message message = ReceiveMessage();
            if (message.GetType() != typeof(Ready_Message))
                return null;
            return (Ready_Message)message;
        }

        /// <summary>
        /// Tries to receive a Data_Message from a device
        /// </summary>
        /// <returns>A Data Message or null</returns>
        public Data_Message ReceiveDataMessage()
        {
            ESP_Message message = ReceiveMessage();
            if (message is null || message.GetType() != typeof(Data_Message))
                return null;
            return (Data_Message)message;
        }

        /// <summary>
        /// Receives an ESP message from the network and parses it
        /// </summary>
        /// <returns>The parsed message</returns>
        private ESP_Message ReceiveMessage()
        {
            IPAddress remoteIPAddress;
            ESP_Message message = null;
            byte header;
            byte[] result = null;

            //Starting the TCP server if not started yet
            if (!TcpServer.Started)
                TcpServer.Start(_localIpAddress, _receivingPort);

            //Connecting to an ESP device
            remoteIPAddress = TcpServer.AcceptNewConnection(_timeout);
            if (!TcpServer.Connected || remoteIPAddress is null)
                return null;

            //Receiving the message header
            header = TcpServer.Receive(1)[0];

            //Parsing the message
            switch (header)
            {
                case Ready_Message.READY_HEADER:
                    try
                    {
                        result = TcpServer.Receive(Ready_Message.PAYLOAD_LENGTH);
                        if (result.Length != Ready_Message.PAYLOAD_LENGTH)
                        {
                            message = null;
                            break;
                        }
                        message = new Ready_Message
                        {
                            Header = Ready_Message.READY_HEADER,
                            Payload = Encoding.ASCII.GetString(result, 0, Ready_Message.PAYLOAD_LENGTH),
                            EspIPAddress = remoteIPAddress
                        };
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        message = null;
                    }
                    break;

                case Data_Message.DATA_HEADER:
                    try
                    {
                        int jsonLenght = -1;
                        if ((result = TcpServer.Receive(1)) is null ||
                            (jsonLenght = result[0]) < 0 ||
                            (result = TcpServer.Receive(result[0])) is null)
                        {
                            message = null;
                            break;
                        }

                        message = new Data_Message
                        {
                            Header = Data_Message.DATA_HEADER,
                            Payload = Encoding.ASCII.GetString(result, 0, jsonLenght),
                            EspIPAddress = remoteIPAddress
                        };
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        message = null;
                    }
                    break;
                default:
                    break;
            }
            TcpServer.CloseConnection();
            return message;
        }
        #endregion

        #region Messages Sending

        /// <summary>
        /// Tries to send a Timestamp Message to a ESP32 <paramref name="device"/>
        /// </summary>
        /// <param name="device">The ESP32 device</param>
        /// <param name="port">The connection port</param>
        /// <returns>True if the message has been sent correctly, False otherwise</returns>
        public bool SendTimestampMessage(ESP32_Device device,int port)
        {
            bool result = false;

            result = TcpServer.Connect(device.Ip, port,Timeout);
            if (result is false)
                return result;

            result = TcpServer.Send(new Timestamp_Message().ToBytes());
            return result;
        }

        /// <summary>
        /// Tries to send a Ok Message to a ESP32 <paramref name="device"/>
        /// </summary>
        /// <param name="device">The ESP32 device</param>
        /// <param name="port">The connection port</param>
        /// <returns>True if the message has been sent correctly, False otherwise</returns>
        public bool SendOkMessage(ESP32_Device device, int port)
        {
            bool result = false;

            result = TcpServer.Connect(device.Ip, port, Timeout);
            if (result is false)
                return result;

            result = TcpServer.Send(new Ok_Message().ToBytes());
            return result;
        }

        #endregion

        #endregion
    }
    */
    #endregion


    #region New ESPManager

    public static class ESPManager
    {
        private static Dictionary<string, ESP32_Device> configuredESP = new Dictionary<string, ESP32_Device>();

        public static void Initialize(List<Device> esps)
        {
            foreach (Device d in esps)
                configuredESP.Add(d.MAC, new ESP32_Device(d) { Active = false });
        }

        public static List<ESP32_Device> ESPs => configuredESP.Values.ToList();

        public static ESP32_Device GetESPDevice(string mac)
        {
            if (configuredESP.ContainsKey(mac))
                return configuredESP[mac];
            else
                return null;
        }

        public static bool IsESPConfigured(string mac) => configuredESP.ContainsKey(mac);

        public static void WaitForMinESPsToConnect()
        {
            while (configuredESP.Where(p => p.Value.Active is true).Count() < 2)
                Thread.Sleep(1000);
        }
    }

    #endregion
}
