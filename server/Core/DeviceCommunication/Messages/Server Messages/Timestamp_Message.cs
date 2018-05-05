using System;

namespace Core.DeviceCommunication.Messages.Server_Messages
{
    public class Timestamp_Message : Server_Message
    {
        public const byte TIMESTAMP_HEADER = 202;

        #region Constructor
        public Timestamp_Message()
        {
            _header = TIMESTAMP_HEADER;
        }
        #endregion

        #region Public Properties
        public new string _payload => DateTime.UtcNow.ToString();
        #endregion
    }
}
