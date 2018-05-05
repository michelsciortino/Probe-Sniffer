using Core.Utilities;
using System;

namespace Core.DeviceCommunication.Messages.Server_Messages
{
    public class Timestamp_Message : Server_Message
    {
        public const string TIMESTAMP_HEADER = "TIMESTAMP";
        #region Constructor
        public Timestamp_Message()
        {
            _header = TIMESTAMP_HEADER;
        }
        #endregion

        #region Public Properties
        public new string _payload => DateTime.UtcNow.ToString();
        #endregion

        #region Public Methods
        public override string ToString() => Util.Stretch(_header, header_lenght) + _payload.Length + _payload;

        #endregion
    }
}
