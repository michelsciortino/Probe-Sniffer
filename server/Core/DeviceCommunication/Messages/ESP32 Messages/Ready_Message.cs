namespace Core.DeviceCommunication.Messages.ESP32_Messages
{
    public class Ready_Message : ESP_Message
    {
        public const byte READY_HEADER = 100;
        public const int PAYLOAD_LENGTH = 17;
    }
}
