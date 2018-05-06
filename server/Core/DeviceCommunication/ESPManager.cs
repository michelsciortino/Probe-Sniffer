using System;
using System.Net;
using System.Text;
using Core.DeviceCommunication.Messages.ESP32_Messages;
using System.Diagnostics;

namespace Core.DeviceCommunication
{
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
        /// <summary>
        /// TCP Listener for Ready Packet
        /// </summary>
        /// <param name="mac">Return by reference MAC of connect device</param>
        /// <param name="ip">Return by reference IP of connect device</param>
        /// <returns>Return true if Ready Packet is correct</returns>
        public Ready_Message ReceiveReadyMessage()
        {
            ESP_Message message = ReceiveMessage();
            if (message.GetType() != typeof(Ready_Message))
                return null;
            return (Ready_Message)message;
        }

        public Data_Message ReceiveDataMessage()
        {
            ESP_Message message = ReceiveMessage();
            if (message.GetType() != typeof(Data_Message))
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
            byte[] payload = null;

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
                        payload = TcpServer.Receive(Ready_Message.PAYLOAD_LENGTH);
                        if (payload.Length != Ready_Message.PAYLOAD_LENGTH)
                        {
                            message = null;
                            break;
                        }
                        message = new Ready_Message
                        {
                            Header = Ready_Message.READY_HEADER,
                            Payload = Encoding.ASCII.GetString(payload, 0, Ready_Message.PAYLOAD_LENGTH),
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
                        byte[] jsonLenght = TcpServer.Receive(1);
                        byte[] json = TcpServer.Receive(jsonLenght[0]);
                        payload = new byte[jsonLenght[0] + 1];
                        Buffer.BlockCopy(jsonLenght, 0, payload, 0, 1);
                        Buffer.BlockCopy(json, 0, payload, 1, jsonLenght[0]);
                        message = new Data_Message
                        {
                            Header = Data_Message.DATA_HEADER,
                            Payload = Encoding.ASCII.GetString(payload, 0, payload.Length),
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

    }
}
