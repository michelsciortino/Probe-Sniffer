using Core.Controls;
using Core.DeviceCommunication.Messages.Server_Messages;
using Core.Models;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Core.DeviceCommunication
{
    public static class UdpBroadcaster
    {
        public const int ESP_PORT = 45445;
        public static UdpClient client = null;

        public static bool Send(byte[] packet)
        {
            try
            {                
                client.Send(packet,packet.Length);
                Debug.WriteLine(BitConverter.ToString(packet));
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
            IPEndPoint rEndPoint = new IPEndPoint(LocalNetworkConnection.GetBroadcastAddress(), ESP_PORT);
            IPEndPoint lEndPoint = new IPEndPoint(LocalNetworkConnection.GetLocalIp(), ESP_PORT);
            try
            {
                while (token.IsCancellationRequested is false)
                {
                    client = new UdpClient(lEndPoint);
                    client.EnableBroadcast = true;
                    client.Connect(rEndPoint);
                    Send(message.ToBytes());
                    client.Close();
                    Thread.Sleep(6000);
                }
            }
            catch
            {
                MessageBox err = new MessageBox("Error creating the socket;\n" + ESP_PORT + " port is already in use.", "Socket exception", icon: System.Windows.MessageBoxImage.Error);
                err.Show();
                return;
            }
            
        }

        public static void Start(CancellationToken token)
        {
            Server_Advertisement_Message sa = new Server_Advertisement_Message();
            Task t = new Task(() => Broadcast(sa, token));
            t.Start();
        }
    }
}
