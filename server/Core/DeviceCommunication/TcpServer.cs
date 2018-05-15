using Core.DeviceCommunication.Messages.ESP32_Messages;
using Core.DeviceCommunication.Messages.Server_Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.DeviceCommunication
{

    #region Old TcpServer
    /*
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
        /// <param name="timeout">The timeout for new connection accepting in seconds (-1 = infinite)</param>
        /// <returns>The IP address of the connected EndPoint</returns>
        public static IPAddress AcceptNewConnection(int timeout)
        {
            //If already connected with a client -> disconnect
            if (_client?.Connected is true || _connected is true)
            {
                if (_client?.Connected is true)
                    _client.Close();
                _connected = false;
            }
            try
            {
                //Waiting for new Connection request
                Stopwatch stopwatch = new Stopwatch();
                if(timeout>0)
                    stopwatch.Start();
                while (true)
                {
                    //If Timed Out -> return null
                    if (timeout> 0 && stopwatch.Elapsed.Seconds > timeout)
                    {
                        stopwatch.Stop();
                        _connected = false;
                        return null;
                    }
                    if (!_listener.Pending())
                        Thread.Sleep(100);
                    else break;
                }
                if(timeout>0)
                stopwatch.Stop();
                
                //Accepting the new Connection
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
        /// Connects to a Remote EndPoint
        /// </summary>
        /// <param name="remoteEndPointAddress">The IP address of the EndPoint</param>
        /// <param name="port">The port for the connection</param>
        /// <param name="timeout">A timeout for the connection request in seconds (-1 = infinite)</param>
        /// <returns></returns>
        public static bool Connect(IPAddress remoteEndPointAddress, int port, int timeout = -1)
        {
            bool result = false;

            if ((bool)_client?.Connected)
            {
                _client.Close();
            }

            Stopwatch stopwatch = new Stopwatch();
            if (timeout > 0)
                stopwatch.Start();
            while (true)
            {
                if (timeout>0 && stopwatch.Elapsed.TotalSeconds > timeout)
                {
                    stopwatch.Stop();
                    result = false;
                    break;
                }
                try
                {
                    _client.Connect(remoteEndPointAddress, port);
                    if (_client.Connected)
                    {
                        result = true;
                        stopwatch.Stop();
                        _connected = _client.Connected;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    result = false;
                }
            }
            return result;
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
                return null;
            }
            return bytes;
        }

        /// <summary>
        /// Sends <paramref name="bytes"/> to the connected Remote EndPoint
        /// </summary>
        /// <param name="bytes">The array of bytes to send</param>
        /// <returns>True if the bytes has been sent, False otherwise</returns>
        public static bool Send(byte[] bytes)
        {
            NetworkStream stream;
            if (_connected)
            {
                try
                {
                    stream = _client.GetStream();
                    stream.Write(bytes,0, bytes.Length);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    return false;
                }
            }
            return true;
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
    */
    #endregion

    #region New TcpServer


    public class TcpServer
    {
        #region Private Members
        private CancellationTokenSource ListenerThreadCancellationTokenSource = null;
        private Mutex MessagesQueueMutex = null;
        private Task ListenerThread = null;
        private TcpListener listener = null;
        private Queue<ESP_Message> MessagesQueue { get; set; }
        #endregion

        #region Public Properties
        public bool IsStarted = false;
        public bool FirstTimestampSent { get; set; } = false;
        public int EnquedMessages
        {
            get
            {
                int n;
                MessagesQueueMutex.WaitOne();
                n = MessagesQueue.Count;
                MessagesQueueMutex.ReleaseMutex();
                return n;
            }
        }
        #endregion

        #region Signals
        public static ManualResetEvent tcpClientConnected = null;
        #endregion


        public TcpServer() {
            MessagesQueue = new Queue<ESP_Message>();
            tcpClientConnected = new ManualResetEvent(false);
            MessagesQueueMutex = new Mutex();
        }

        public void Start(IPAddress localIp,int port)
        {
            if (IsStarted) return;
            ListenerThreadCancellationTokenSource = new CancellationTokenSource();
            try
            {
                ListenerThread = new Task(() => ListenerCallBack(localIp, port,ListenerThreadCancellationTokenSource.Token));
                IsStarted = true;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
                IsStarted = false;
            }
        }

        private void ListenerCallBack(IPAddress localIp, int port, CancellationToken token)
        {
            TcpListener listener = null;
            IPEndPoint localEndPoint = null;
            byte[] bytes = new Byte[1024];

            try { localEndPoint = new IPEndPoint(localIp, port); }
            catch { return; }

            try { listener = new TcpListener(localIp, port); }
            catch { return; }

            try
            {
                listener.Start();
                while (true)
                {
                    if (token.IsCancellationRequested) break;
                    tcpClientConnected.Reset();
                    listener.BeginAcceptTcpClient(new AsyncCallback(AcceptCallback), listener);
                    tcpClientConnected.WaitOne();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return;
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            TcpListener listener = (TcpListener)ar.AsyncState;
            TcpClient client = listener.EndAcceptTcpClient(ar);
            Task serveClient = new Task(() => ServeClient(client));
            serveClient.Start();
            tcpClientConnected.Set();
        }

        private void ServeClient(TcpClient client)
        {
            int headerCode;
            byte[] bytes;

            ESP_Message message;

            while (true)
            {
                bytes=Receive(client,1);
                if (bytes == null) break;
                headerCode = bytes[0];

                message = null;

                switch (headerCode)
                {
                    case Ready_Message.READY_HEADER:
                        try
                        {
                            bytes = Receive(client,Ready_Message.PAYLOAD_LENGTH);
                            if (bytes == null) return;
                            if (bytes.Length != Ready_Message.PAYLOAD_LENGTH)
                            {
                                message = null;
                                break;
                            }
                            message = new Ready_Message
                            {
                                Header = Ready_Message.READY_HEADER,
                                Payload = Encoding.ASCII.GetString(bytes, 0, Ready_Message.PAYLOAD_LENGTH)
                            };
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            message = null;
                        }
                        break;

                    case Data_Message.DATA_HEADER:
                        try
                        {
                            int jsonLenght = -1;
                            if ((bytes = Receive(client,1)) is null ||
                                (jsonLenght = bytes[0]) < 0 ||
                                (bytes = Receive(client,bytes[0])) is null)
                            {
                                message = null;
                                break;
                            }

                            message = new Data_Message
                            {
                                Header = Data_Message.DATA_HEADER,
                                Payload = Encoding.ASCII.GetString(bytes, 0, jsonLenght)
                            };
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            message = null;
                        }
                        break;
                    default:
                        break;
                }
                if(message!=null)
                {
                    MessagesQueueMutex.WaitOne();
                    MessagesQueue.Enqueue(message);
                    MessagesQueueMutex.ReleaseMutex();
                }
            }
        }

        /// <summary>
        /// Receives <paramref name="nBytes"/> from the EndPoint
        /// </summary>
        /// <param name="nBytes">Number of bytes to receive</param>
        /// <returns>The received bytes</returns>
        private static byte[] Receive(TcpClient client,int nBytes)
        {

            byte[] bytes = new byte[nBytes];
            int readBytes = 0, leftBytes = nBytes;

            try
            {
                NetworkStream stream = client.GetStream();
                while (leftBytes < nBytes)
                {
                    readBytes = stream.Read(bytes, nBytes - leftBytes, nBytes);
                    leftBytes -= readBytes;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
            return bytes;
        }

        public Queue<ESP_Message> GetNewMessages(int max)
        {
            Queue<ESP_Message> messages = new Queue<ESP_Message>();

            MessagesQueueMutex.WaitOne();
            for (int i = 0; i < max; i++)
            {
                if (MessagesQueue.Count > 0)
                {
                    try { messages.Enqueue(MessagesQueue.Dequeue()); }
                    catch { break; }
                }
                else
                    break;
            }           
            MessagesQueueMutex.ReleaseMutex();
            return messages;
        }

        public bool SendMessage(IPEndPoint remoteEsp,Server_Message message)
        {
            try
            {
                TcpClient client = new TcpClient();
                if(!client.ConnectAsync(remoteEsp.Address, remoteEsp.Port).Wait(1000))
                    return false;
                Send(client, message.ToBytes());
            }
            catch
            {
                return false;
            }
            return true;
        }


        /// <summary>
        /// Sends <paramref name="bytes"/> to the connected Remote EndPoint
        /// </summary>
        /// <param name="bytes">The array of bytes to send</param>
        /// <returns>True if the bytes has been sent, False otherwise</returns>
        private static bool Send(TcpClient client, byte[] bytes)
        {
            NetworkStream stream;

            try
            {
                stream = client.GetStream();
                stream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
            return true;
        }
    }


    #endregion
}
