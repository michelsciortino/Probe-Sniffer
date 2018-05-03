namespace Core.DeviceCommunication.Messages.Server_Messages
{
    public abstract class Server_Message
    {
        protected const int header_lenght = 10;

        #region Private Properties
        protected string _header;
        protected string _payload;
        #endregion

        #region Public Properties
        public string Header => _header;
        public string Payload => _payload;
        #endregion

        #region Public Methods
        public new abstract string ToString();
        #endregion
    }
}
