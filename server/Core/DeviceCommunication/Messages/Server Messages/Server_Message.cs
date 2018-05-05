namespace Core.DeviceCommunication.Messages.Server_Messages
{
    public abstract class Server_Message
    {
        #region Private Properties
        protected byte _header;
        protected string _payload;
        #endregion

        #region Public Properties
        public byte Header => _header;
        public string Payload => _payload;
        #endregion

        #region Public Methods
        public abstract byte[] ToBytes();
        #endregion
    }
}
