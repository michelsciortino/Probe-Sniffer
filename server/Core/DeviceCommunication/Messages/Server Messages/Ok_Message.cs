using Core.Utilities;
namespace Core.DeviceCommunication.Messages.Server_Messages
{
    class Ok_Message : Server_Message
    {
        public const string OK_HEADER_STRING = "OK";
        public const byte OK_HEADER = 200;

        #region Constructor
        public Ok_Message()
        {
            _header = OK_HEADER;
            _payload = "";
        }
        #endregion
    }
}
