using Core.Utilities;
namespace Core.DeviceCommunication.Messages.Server_Messages
{
    class Ok_Message : Server_Message
    {
        public const string OK_HEADER = "OK";

        #region Constructor
        public Ok_Message()
        {   
            _header = OK_HEADER;
        }
        #endregion

        #region Public Methods
        public override string ToString() => Util.Stretch(_header, header_lenght);
        #endregion
    }
}
