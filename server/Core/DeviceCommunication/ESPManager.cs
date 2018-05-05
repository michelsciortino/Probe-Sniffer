using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Core.Models;
using Core.DeviceCommunication.Messages.ESP32_Messages;

namespace Core.DeviceCommunication
{
    public static class ESPManager
    {
        /// <summary>
        /// TCP Listener for Ready Packet
        /// </summary>
        /// <param name="mac">Return by reference MAC of connect device</param>
        /// <param name="ip">Return by reference IP of connect device</param>
        /// <returns>Return true if Ready Packet is correct</returns>
        public static bool ReceiveDeviceReady(ref string mac, ref IPAddress ip)
        {
            TcpListener server = null;
            bool result = false;

            try
            {
                int port = DeviceCommunication.SERVER_PORT;
                IPAddress LocalIP = LocalNetworkConnection.GetLocalIp();

                server = new TcpListener(LocalIP, port);

                server.Start();

                TcpClient device = server.AcceptTcpClient();

                IPEndPoint DeviceEndPoint = (IPEndPoint)device.Client.RemoteEndPoint;
                ip = DeviceEndPoint.Address;

                Byte[] bytesbuffer = new Byte[device.ReceiveBufferSize];

                //stream for read data
                NetworkStream stream = device.GetStream();
                int bytesRead;

                //loop to receive data
                while ((bytesRead = stream.Read(bytesbuffer, 0, bytesbuffer.Length)) != 0)
                {
                    if (ManageCaseBytes(bytesbuffer, bytesRead, ref mac) == false)
                    {
                        result = false;
                        return result;
                    }
                }
                device.Close();
                result = true;
            }
            catch (SocketException)
            {
                result = false;
            }
            finally
            {
                server.Stop();
            }
            return result;
        }

        public static void ReceiveDeviceData(ref string mac)
        {

        }

        /// <summary>
        /// To Manage different packet 
        /// </summary>
        /// <param name="buffer">Buffer read in Bytes</param>
        /// <param name="bytesRead">Number of Bytes read</param>
        /// <param name="mac">return MAC of device</param>
        /// <returns>Return true if packet is correct</returns>
        private static bool ManageCaseBytes(byte[] buffer, int bytesRead, ref string mac)
        {
            bool result;
            switch (buffer[0])
            {
                case Ready_Message.READY_HEADER:   //204 -> READY Packet
                    mac = Encoding.ASCII.GetString(buffer, 1, bytesRead - 1);
                    if (mac.Length != 17)
                    {
                        result = false;
                    }
                    else result = true;
                    break;

                default:
                    result = false;
                    break;
            }
            return result;
        }
    }
}
