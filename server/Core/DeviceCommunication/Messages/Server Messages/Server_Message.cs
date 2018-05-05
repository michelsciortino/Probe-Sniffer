using System;
using System.Text;

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
        public byte[] ToBytes()
        {
            byte[] bytes = new Byte[_payload.Length + 1];
            bytes[0] = _header;
            byte[] payloadBytes = Encoding.ASCII.GetBytes(_payload.ToCharArray(), 0, _payload.Length);
            Buffer.BlockCopy(payloadBytes, 0, bytes, 1, payloadBytes.Length);
            return bytes;
        }
        #endregion
    }
}
