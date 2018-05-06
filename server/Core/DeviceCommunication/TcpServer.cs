using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Core.DeviceCommunication
{
    public static class TcpServer
    {

        #region Private Properties
        private static TcpListener _listener = null;
        private static TcpClient _client = null;
        private static IPAddress _remoteEndPointAddress = null;
        private static bool _started = false;
        private static bool _connected = false;
        #endregion

        #region Public Properties
        public static IPAddress RemoteEndPointAddress => _remoteEndPointAddress;
        public static bool Started => _started;
        public static bool Connected => _connected;
        #endregion

        #region Public Methods
        /// <summary>
        /// Starts the listening for new connections
        /// </summary>
        /// <returns>True if started, False otherwise</returns>
        public static bool Start(IPAddress localIP, int port)
        {
            if (_started) return _started;
            try
            {
                _listener = new TcpListener(localIP, port);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
            try
            {
                _listener.Start();
                _started = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _started = false;
            }
            return _started;
        }

        /// <summary>
        /// Accepts a new connection
        /// </summary>
        /// <returns>The IP address of the connected EndPoint</returns>
        public static IPAddress AcceptNewConnection()
        {
            if (_client?.Connected is true || _connected is true)
            {
                if (_client?.Connected is true)
                    _client.Close();
                _connected = false;
            }
            try
            {
                _client = _listener.AcceptTcpClient();
                _remoteEndPointAddress = ((IPEndPoint)_client.Client.RemoteEndPoint).Address;
                _connected = _client.Connected;
            }
            catch (Exception)
            {
                _remoteEndPointAddress = null;
                _connected = (bool)_client?.Connected;
            }
            return _remoteEndPointAddress;
        }

        /// <summary>
        /// Receives <paramref name="nBytes"/> from the EndPoint
        /// </summary>
        /// <param name="nBytes">Number of bytes to receive</param>
        /// <returns>The received bytes</returns>
        public static byte[] Receive(int nBytes)
        {
            if (_started is false)
                throw new Exception("TcpReceiver not started");
            if (_connected is false)
                throw new Exception("TcpReceiver not not connected to an EndPoint");

            byte[] bytes = new byte[nBytes];
            int readBytes = 0, leftBytes = nBytes;

            try
            {
                //stream for read data
                NetworkStream stream = _client.GetStream();
                while (leftBytes < nBytes)
                {
                    readBytes = stream.Read(bytes, nBytes - leftBytes, nBytes);
                    leftBytes -= readBytes;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
            return bytes;
        }

        /// <summary>
        /// Closes the connection with the current connected client (if any)
        /// </summary>
        public static void CloseConnection()
        {
            if ((bool)_client?.Connected)
                _client.Close();
        }

        /// <summary>
        /// Stops the TcpListener
        /// </summary>
        public static void StopListener()
        {
            try
            {
                _listener.Stop();
                _started = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        #endregion

    }
}
