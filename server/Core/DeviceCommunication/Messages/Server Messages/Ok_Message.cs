namespace Core.DeviceCommunication.Messages.Server_Messages
{
    class Ok_Message : Server_Message
    {
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
