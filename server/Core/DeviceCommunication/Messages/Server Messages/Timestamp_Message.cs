using System;
using System.Text;

namespace Core.DeviceCommunication.Messages.Server_Messages
{
    public class Timestamp_Message : Server_Message
    {
        public const byte TIMESTAMP_HEADER = 202;

        #region Constructor
        public Timestamp_Message()
        {
            _header = TIMESTAMP_HEADER;
        }
        #endregion

        #region Public Properties
        public new string _payload;
        #endregion

        public new byte[] ToBytes()
        {
            _payload= DateTime.UtcNow.ToString();
            byte[] bytes = new Byte[_payload.Length + 1];
            bytes[0] = _header;
            byte[] payloadBytes = Encoding.ASCII.GetBytes(_payload.ToCharArray(), 0, _payload.Length);
            Buffer.BlockCopy(payloadBytes, 0, bytes, 1, payloadBytes.Length);
            return bytes;
        }
    }
}
