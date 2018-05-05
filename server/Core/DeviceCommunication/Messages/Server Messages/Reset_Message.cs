namespace Core.DeviceCommunication.Messages.Server_Messages
{
    public class Reset_Message : Server_Message
    {
        public const byte RESET_HEADER = 203;

        #region Constructor
        public Reset_Message()
        {
            _header = RESET_HEADER;
            _payload = "";
        }
        #endregion
    }
}
