namespace Core.DeviceCommunication.Messages.ESP32_Messages
{
    public class Ready_Message : ESP_Message
    {
        public const string READY_HEADER_STRING = "READY";
        public const byte READY_HEADER = 204;
    }
}
