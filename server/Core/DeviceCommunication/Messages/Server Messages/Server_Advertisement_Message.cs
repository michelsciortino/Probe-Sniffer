using Core.Models;
using Core.Utilities;
using System.Net;

namespace Core.DeviceCommunication.Messages.Server_Messages
{
    public class Server_Advertisement_Message : Server_Message
    {
        public const string SERVER_ADV = "SERVER_ADV";

        #region Constructor
        public Server_Advertisement_Message()
        {
            IPAddress localIP = LocalNetworkConnection.GetLocalIp();
            string ip = localIP.ToString();
            _header = SERVER_ADV;
            _payload = ip.Length.ToString() + ip;
        }
        #endregion

        #region Public Methods
        public override string ToString()
        {
            return Util.Stretch(_header, 10) + _payload;
        }
        #endregion
    }
}
