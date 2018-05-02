using System.Net;

namespace Core.Models.DeviceMessages
{
    class Server_Advertisement
    {
        public string Message => AdvertisementCreator();

        private static string AdvertisementCreator()
        {
            string message = "";

            IPAddress localIP = LocalNetworkConnection.GetLocalIp();
            message = "SERVER_ADV" + localIP.ToString();

            return message;
        }
    }
}
