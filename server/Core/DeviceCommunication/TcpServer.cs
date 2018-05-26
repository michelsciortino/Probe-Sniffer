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

    public enum ServerMode
    {
        DATACOLLECTION_MODE =0,
        CONFIGURATION_MODE
    }

    public class TcpServer
    {
        public const int SERVER_PORT = 48448, SYNC_INTERVAL = 20000;

        #region Private Members
        private ServerMode mode;
        private CancellationTokenSource ListenerThreadCancellationTokenSource = null;
        private Mutex messagesQueueMutex = null;
        private Mutex canReceiveDataMutex = null;
        private Mutex clientsListMutex = null;
        private Task listenerTask = null;
        private TcpListener listener = null;
        private Queue<ESP_Message> messagesQueue = null;
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
        public TcpServer(ServerMode mode)
        {
            this.mode = mode;
            messagesQueue = new Queue<ESP_Message>();
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
                listenerTask = new Task(() => ListenerCallBack(localIp, SERVER_PORT, ListenerThreadCancellationTokenSource.Token));
                listenerTask.Start();
                _isStarted = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _isStarted = false;
            }
        }

        public Queue<ESP_Message> GetNewMessages()
        {
            Queue<ESP_Message> messages = new Queue<ESP_Message>();

            messagesQueueMutex.WaitOne();
            while (messagesQueue.Count > 0)
            {
                try { messages.Enqueue(messagesQueue.Dequeue()); }
                catch { break; }
            }
            messagesQueueMutex.ReleaseMutex();
            return messages;
        }

        public ESP_Message GetNextMessage()
        {
            ESP_Message ret = null;
            messagesQueueMutex.WaitOne();
            if (messagesQueue.Count > 0)
                ret = messagesQueue.Dequeue();
            messagesQueueMutex.ReleaseMutex();
            return ret;
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
                bytes = await ReceiveAsync(client, 1);
                if (bytes == null)
                    if (client.Connected)
                        continue;
                    else
                        break;
                headerCode = bytes[0];

                message = null;

                switch (headerCode)
                {
                    case Ready_Message.READY_HEADER:
                        try
                        {
                            bytes = await ReceiveAsync(client, Ready_Message.PAYLOAD_LENGTH);
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
                            bytes = await ReceiveAsync(client, 2);
                            if(bytes is null)
                            {
                                message = null;
                                break;
                            }
                            jsonLenght = BitConverter.ToUInt16(bytes,0);

                            bytes = await ReceiveAsync(client, jsonLenght);
                            if (bytes is null)
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
                    if (mode == ServerMode.CONFIGURATION_MODE)
                        EnqueueMessage(message);
                    else if (mode == ServerMode.DATACOLLECTION_MODE)
                        if (ESPManager.IsESPConfigured(message.Payload))
                        {
                            if (esp is null)
                            {
                                esp = ESPManager.GetESPDevice(message.Payload);
                                Logger.Log("New ESP32 connection\t\tx: " + esp?.X_Position + " y: " + esp?.Y_Position + "\r\n");
                                ESPManager.SetDeviceStatus(esp.MAC, true);
                            }
                            if (CanReceiveData is true)
                                Send(client, new Ok_Message().ToBytes());
                        }
                        else
                            break;
                }
                else if (message is Data_Message && mode == ServerMode.DATACOLLECTION_MODE)
                    EnqueueMessage(message);
            }
            client.Close();
            if (esp != null)
                Logger.Log("An ESP disconnected\t\tx: " + esp.X_Position + " y: " + esp.Y_Position + "\r\n");
            KillZombies();
            ESPManager.SetDeviceStatus(esp?.MAC, false);
        }

        private void EnqueueMessage(ESP_Message message)
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
}
