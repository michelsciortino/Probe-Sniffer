using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Core.DeviceCommunication
{
    public static class UDPsender
    {
        public static bool Send(IPAddress dest, int port, byte[] packet)
        {
            try
            {
                UdpClient client = new UdpClient();
                IPEndPoint endPoint = new IPEndPoint(dest, port);
                client.EnableBroadcast = true;
                client.Send(packet, packet.Length, endPoint);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
            return true;
        }
    }
}
