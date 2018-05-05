namespace Core.DeviceCommunication.Messages.ESP32_Messages
{
    public abstract class ESP_Message
    {
        protected const int header_lenght = 10;

        #region Private Properties
        protected string _header;
        protected string _payload;
        #endregion

        #region Public Properties
        public string Header
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
