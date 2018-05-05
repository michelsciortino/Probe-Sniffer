using Core.Utilities;
using System;

namespace Core.DeviceCommunication.Messages.Server_Messages
{
    public class Timestamp_Message : Server_Message
    {
        public const string TIMESTAMP_HEADER_STRING = "TIMESTAMP";
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

        #region Public Methods
        public override byte[] ToBytes() => Util.MessageInBytes(TIMESTAMP_HEADER, _payload);
        #endregion
    }
}
