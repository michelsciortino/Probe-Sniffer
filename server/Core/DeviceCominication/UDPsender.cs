using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Core.DeviceCommunication
{
    public static class UDPsender
    {
        public static bool Send(IPAddress dest,int port,string packet)
        {
            try
            {
                Byte[] sendBytes = Encoding.ASCII.GetBytes(packet.ToCharArray(), 0, packet.Length);
                UdpClient client = new UdpClient();
                IPEndPoint endPoint = new IPEndPoint(dest, port);
                client.EnableBroadcast = true;
                client.Send(sendBytes, sendBytes.Length, endPoint);
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
            return true;
        }
    }
}
