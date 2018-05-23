using Core.DeviceCommunication.Messages.ESP32_Messages;
using Core.DeviceCommunication.Messages.Server_Messages;
using Core.Models;
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
                Logger.Log(ex.Message);
                throw;
            }
            try
            {
                _listener.Start();
                _started = true;
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message);
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
                    Logger.Log(ex.Message);
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
                Logger.Log(ex.Message);
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
                    Logger.Log(ex.Message);
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
                Logger.Log(ex.Message);
            }
        }
        #endregion

    }
    */
    #endregion

    #region New TcpServer


    public class TcpServer
    {
        public const int SERVER_PORT = 48448, SYNC_INTERVAL = 20000;

        #region Private Members
        private CancellationTokenSource ListenerThreadCancellationTokenSource = null;
        private Mutex messagesQueueMutex = null;
        private Mutex canReceiveDataMutex = null;
        private Mutex clientsListMutex = null;
        private Task listenerTask = null;
        private TcpListener listener = null;
        private Queue<Data_Message> messagesQueue = null;
        private List<TcpClient> clients = null;
        private Timer syncronizer = null;
        #endregion

        #region Private Properties
        private bool _canReceiveData = false;
        private bool _isStarted = false;
        #endregion

        #region Public Properties
        public bool IsStarted => _isStarted;

        public bool CanReceiveData
        {
            get => _canReceiveData;
            set
            {
                canReceiveDataMutex.WaitOne();
                _canReceiveData = value;
                try
                {
                    if (value is true)
                    {
                        clientsListMutex.WaitOne();
                        foreach (var c in clients) Send(c, new Ok_Message().ToBytes());
                        clientsListMutex.ReleaseMutex();
                        syncronizer.Change(0, SYNC_INTERVAL);
                    }
                    else
                        syncronizer.Change(Timeout.Infinite, Timeout.Infinite);
                }
                catch(Exception ex) { Debug.WriteLine(ex.Message); }
                canReceiveDataMutex.ReleaseMutex();
            }
        }

        public int EnquedMessages
        {
            get
            {
                int n;
                messagesQueueMutex.WaitOne();
                n = messagesQueue.Count;
                messagesQueueMutex.ReleaseMutex();
                return n;
            }
        }
        #endregion

        #region Signals
        public static ManualResetEvent tcpClientConnected = null;
        #endregion

        #region Constructor
        public TcpServer()
        {
            messagesQueue = new Queue<Data_Message>();
            tcpClientConnected = new ManualResetEvent(false);
            messagesQueueMutex = new Mutex();
            canReceiveDataMutex = new Mutex();
            clientsListMutex = new Mutex();
            clients = new List<TcpClient>();
            syncronizer = new Timer(SyncronizeClients, null, Timeout.Infinite, Timeout.Infinite);
        }
        #endregion

        #region Public Methods
        public void Start()
        {
            if (IsStarted) return;
            IPAddress localIp = LocalNetworkConnection.GetLocalIp();
            ListenerThreadCancellationTokenSource = new CancellationTokenSource();
            try
            {
                listenerTask = new Task(() => ListenerCallBack(localIp, SERVER_PORT,ListenerThreadCancellationTokenSource.Token));
                listenerTask.Start();
                _isStarted = true;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _isStarted = false;
            }
        }

        public Queue<Data_Message> GetNewMessages()
        {
            Queue<Data_Message> messages = new Queue<Data_Message>();

            messagesQueueMutex.WaitOne();
            while (messagesQueue.Count > 0)
            {
                try { messages.Enqueue(messagesQueue.Dequeue()); }
                catch { break; }
            }
            messagesQueueMutex.ReleaseMutex();
            return messages;
        }
        #endregion

        #region Private Methods

        private void ListenerCallBack(IPAddress localIp, int port, CancellationToken token)
        {
            listener = null;
            IPEndPoint localEndPoint = null;

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
                Debug.WriteLine(ex.Message);
                return;
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            TcpListener listener = (TcpListener)ar.AsyncState;
            TcpClient client = listener.EndAcceptTcpClient(ar);
            clientsListMutex.WaitOne();
            clients.Add(client);
            clientsListMutex.ReleaseMutex();
            ServeClientAsync(client);
            tcpClientConnected.Set();
        }

        private async Task ServeClientAsync(TcpClient client)
        {
            int headerCode;
            byte[] bytes;
            ESP_Message message;
            ESP32_Device esp = null;
            while (true)
            {
                bytes= await ReceiveAsync(client,1);
                if (bytes == null)
                    if (client.Connected) continue;
                    else break;
                headerCode = bytes[0];

                message = null;

                switch (headerCode)
                {
                    case Ready_Message.READY_HEADER:
                        try
                        {
                            bytes = await ReceiveAsync(client,Ready_Message.PAYLOAD_LENGTH);
                            if (bytes == null)
                                break;
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
                            break;
                        }
                        break;

                    case Data_Message.DATA_HEADER:
                        try
                        {
                            int jsonLenght = -1;
                            if ((bytes = await ReceiveAsync(client,1)) is null ||
                                (jsonLenght = bytes[0]) < 0 ||
                                (bytes = await ReceiveAsync(client,bytes[0])) is null)
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
                            break;
                        }
                        break;
                    default:
                        break;
                }

                if (message is null)
                    break;
                if (message is Ready_Message)
                {
                    if (ESPManager.IsESPConfigured(message.Payload))
                    {
                        if(esp is null)
                        {
                            esp = ESPManager.GetESPDevice(message.Payload);
                            Logger.Log("New ESP32 connection\t\tx: " + esp?.X_Position + " y: " + esp?.Y_Position + "\r\n");
                        }
                        if (CanReceiveData is true)
                            Send(client, new Ok_Message().ToBytes());
                    }
                    else
                        break;
                }
                else if (message is Data_Message) EnqueueMessage(message as Data_Message);
            }
            client.Close();
            if(esp!=null)
                Logger.Log("An ESP disconnected\t\tx: " + esp.X_Position + " y: " + esp.Y_Position + "\r\n");
            KillZombies();
        }

        private void EnqueueMessage(Data_Message message)
        {
            messagesQueueMutex.WaitOne();
            messagesQueue.Enqueue(message);
            messagesQueueMutex.ReleaseMutex();
        }

        /// <summary>
        /// Receives <paramref name="nBytes"/> from the EndPoint
        /// </summary>
        /// <param name="nBytes">Number of bytes to receive</param>
        /// <returns>The received bytes</returns>
        private static async Task<byte[]> ReceiveAsync(TcpClient client,int nBytes)
        {

            byte[] bytes = new byte[nBytes];
            int readBytes = 0, leftBytes = nBytes;

            try
            {
                NetworkStream stream = client.GetStream();
                while (leftBytes > 0)
                {
                    readBytes = await stream.ReadAsync(bytes, nBytes - leftBytes, nBytes);
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

        private void SyncronizeClients(object state)
        {
            clientsListMutex.WaitOne();
            foreach(TcpClient c in clients)
                if(SendTimestamp(c) is false) c.Close();
            clientsListMutex.ReleaseMutex();
        }

        private static bool SendTimestamp(TcpClient client)
        {
            NetworkStream stream;

            try
            {
                stream = client.GetStream();
                Timestamp_Message message = new Timestamp_Message();
                byte[] bytes = message.ToBytes();
                stream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        private void KillZombies()
        {
            clientsListMutex.WaitOne();
            clients.RemoveAll(c => c.Connected is false);
            clientsListMutex.ReleaseMutex();
        }
        #endregion
    }
    #endregion
}
