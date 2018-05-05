using Core.Utilities;
namespace Core.DeviceCommunication.Messages.Server_Messages
{
    public class Reset_Message : Server_Message
    {
        public const string RESET_HEADER = "RESET";

        #region Constructor
        public Reset_Message()
        {
            _header = RESET_HEADER;
        }
        #endregion

        #region Public Methods
        public override string ToString() => Util.Stretch(_header, header_lenght);
        #endregion
    }
}
