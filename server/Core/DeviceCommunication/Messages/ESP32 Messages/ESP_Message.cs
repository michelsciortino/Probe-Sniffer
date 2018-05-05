namespace Core.DeviceCommunication.Messages.ESP32_Messages
{
    public abstract class ESP_Message
    {
        #region Private Properties
        protected byte _header;
        protected string _payload;
        #endregion

        #region Public Properties
        public byte Header
        {
            get => _header;
            set => _header = value;
        }
        public string Payload
        {
            get => _payload;
            set => _payload = value;
        }
        #endregion
    }
}
