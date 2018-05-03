using Core.Models;
using static Core.Utilities.Utilities;
using System.Net;

namespace Core.DeviceCommunication.Messages
{
    class Server_Advertisement
    {
        private const string SERVER_ADV = "SERVER_ADV";

        #region Private Properties
        private string _payload = null;
        private string _header = null;
        #endregion

        #region Constructor
        public Server_Advertisement()
        {
            IPAddress localIP = LocalNetworkConnection.GetLocalIp();
            string ip = localIP.ToString();
            _header = SERVER_ADV;
            _payload = ip.Length.ToString() + ip;
        }
        #endregion

        #region Public Properties
        public string Header => _header;
        public string Payload => _payload;
        #endregion

        #region Public Methods
        public override string ToString()
        {
            return Stretch(_header, 10) + _payload;
        }
        #endregion
    }
}
