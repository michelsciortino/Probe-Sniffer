using static Core.Utilities.Utilities;
namespace Core.DeviceCommunication.Messages
{
    class Ok_Message
    {
        private const string OK_MESSAGE = "OK";

        #region Private Properties
        private string _payload = null;
        private string _header = null;
        #endregion

        #region Constructor
        public Ok_Message()
        {   
            _header = OK_MESSAGE;
        }
        #endregion

        #region Public Properties
        public string Header => _header;
        public string Payload => _payload;
        #endregion

        #region Public Methods
        public override string ToString() => Stretch(_header, 10);
        #endregion
    }
}
