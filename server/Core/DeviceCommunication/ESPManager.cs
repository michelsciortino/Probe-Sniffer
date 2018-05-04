using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Core.DeviceCommunication
{
    public static class ESPManager
    {
        public static bool ReceiveDeviceReady(ref string mac, ref IPAddress ip)
        {
            TcpListener server = null;
            bool result = false;

            try
            {
                int port = DeviceCommunication.SERVER_PORT;   //porta dove ricevere
                IPAddress LocalIP = Core.Models.LocalNetworkConnection.GetLocalIp(); //mettere ip dove ricevere

                server = new TcpListener(LocalIP, port);

                server.Start();

                TcpClient device = server.AcceptTcpClient();

                Byte[] bytesbuffer = new Byte[device.ReceiveBufferSize]; 
                string data = null;

                //faccio uno stream per leggere
                NetworkStream stream = device.GetStream();
                int bytesRead;

                //loop to receive data
                while ((bytesRead = stream.Read(bytesbuffer, 0, bytesbuffer.Length)) != 0)
                {
                    data = Encoding.ASCII.GetString(bytesbuffer, 0, bytesRead);
                    Console.WriteLine("received: ", data);

                    string header = data.Substring(0,10);  //header length packet (10 char)
                    ManageCase(header, data, ref mac);
                }
                device.Close();
                result = true;
            }
            catch (SocketException e)
            {
                result = false;
            }
            finally
            {
                server.Stop();
            }
            return result;
        }

        public static void ManageCase(string header, string data, ref string mac)
        {
            switch (header)
            {
                case "READY     ":
                    //Estraggo MAC da data
                    mac = data.Substring(10, 17);   //sostituire con cose giuste
                    break;

                case "DATA      ":
                    break;

                default:
                    break;
            }
        }
    }
}
