using Core.DeviceCommunication.Messages.Server_Messages;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Core.DeviceCommunication
{
    public static class UdpBroadcaster
    {
        public const int ESP_PORT = 45445;
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

        public static void Broadcast(Server_Message message,CancellationToken token)
        {
            while(token.IsCancellationRequested is false)
            {
                Send(IPAddress.Broadcast, ESP_PORT, message.ToBytes());
                Thread.Sleep(5000);
            }
        }
    }
}
