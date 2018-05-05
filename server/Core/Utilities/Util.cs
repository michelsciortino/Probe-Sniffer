using System;
using System.Text;

namespace Core.Utilities
{
    public static class Util
    {
        /// <summary>
        /// Concat header in byte with paylod in bytes
        /// </summary>
        /// <param name="header">Header in byte</param>
        /// <param name="payload">Payload to convert in bytes and concat</param>
        /// <returns>Return message in bytes</returns>
        public static byte[] MessageInBytes(byte header, string payload)
        {
            byte[] message = new Byte[payload.Length + 1];
            message[0] = header;
            byte[] payloadBytes = Encoding.ASCII.GetBytes(payload.ToCharArray(), 0, payload.Length);
            Buffer.BlockCopy(message, 0, payloadBytes, 1, payloadBytes.Length);
            return message;
        }
    }
}