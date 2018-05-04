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

            try
            {
                int port = DeviceCommunication.SERVER_PORT;   //porta dove ricevere
                IPAddress LocalIP = Core.Models.LocalNetworkConnection.GetLocalIp(); //mettere ip dove ricevere

                server = new TcpListener(LocalIP, port);

                server.Start();

                Byte[] bytesbuffer = new Byte[27];  //10(header)+17(mac)    METTERE COME CAMPO NELLE CLASSI MESSAGGI LA LUNGHEZZA FINALE SE C'E'
                string data = null;

                //listening loop
                while (true)
                {
                    Console.WriteLine("Waiting for connection...");
                    TcpClient device = server.AcceptTcpClient();
                    Console.WriteLine("connected!");

                    data = null;

                    //faccio uno stream per leggere
                    NetworkStream stream = device.GetStream();
                    int i;

                    //loop to receive data
                    while ((i = stream.Read(bytesbuffer, 0, bytesbuffer.Length)) != 0)
                    {
                        data = Encoding.ASCII.GetString(bytesbuffer, 0, i);
                        Console.WriteLine("received: ", data);

                        //Estraggo MAC da data
                        //da aggiustare non va
                        mac = data.Substring(11, 17);

                        //mandiamo già quà OK? o lasciamo fuori?
                        //byte[] ok = Encoding.ASCII.GetBytes("ok");
                        //stream.Write(ok,0,ok.Length);
                    }
                    device.Close();
                }
                return true;
            }
            catch (SocketException e)
            {
                return false;
            }
            finally
            {
                server.Stop();
            }
            return true;
        }
    }
}
