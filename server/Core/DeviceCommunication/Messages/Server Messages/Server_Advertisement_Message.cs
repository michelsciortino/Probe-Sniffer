using Core.Models;
using System.Net;

namespace Core.DeviceCommunication.Messages.Server_Messages
{
    public class Server_Advertisement_Message : Server_Message
    {
        public const string SERVER_ADV_STRING = "SERVER_ADV";
        public const byte SERVER_ADV = 201;

        #region Constructor
        public Server_Advertisement_Message()
        {
            IPAddress localIP = LocalNetworkConnection.GetLocalIp();
            string ip = localIP.ToString();
            _header = SERVER_ADV;
            _payload = ip.Length.ToString() + ip;
        }
        #endregion
    }
}
