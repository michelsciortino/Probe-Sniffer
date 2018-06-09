using Core.DeviceCommunication.Messages.ESP32_Messages;
using Core.DeviceCommunication.Messages.Server_Messages;
using Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.DeviceCommunication
{

    public class TcpServer2
    {
        public const int SERVER_PORT = 48448, SYNC_INTERVAL = 30000;

        #region Private Members
        private ServerMode mode;
        private Socket listener = null;
        private Timer syncronizer = null;
        private Thread listenerThread = null;
        private Mutex connectionsMutex = null;
        public Mutex messagesQueueMutex = null;
        public Mutex canReceiveDataMutex = null;
        private List<Socket> ConnectedEsps = null;
        public AutoResetEvent NewMessageEvent = null;
        private Queue<ESP_Message> messagesQueue = null;
        private ManualResetEventSlim EspConnectedEvent = null;
        private CancellationTokenSource StopServer = null;
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
                        syncronizer.Change(0, SYNC_INTERVAL);
                    }
                    else
                        syncronizer.Change(Timeout.Infinite, Timeout.Infinite);
                }
                catch (Exception ex) { Debug.WriteLine(ex.Message); }
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

        #region Constructor
        public TcpServer2(ServerMode mode)
        {
            this.mode = mode;
            connectionsMutex = new Mutex();
            messagesQueueMutex = new Mutex();
            canReceiveDataMutex = new Mutex();
            messagesQueue = new Queue<ESP_Message>();
            ConnectedEsps = new List<Socket>();
            syncronizer = new Timer(SyncronizeClients, null, Timeout.Infinite, Timeout.Infinite);
            EspConnectedEvent=new ManualResetEventSlim();
            NewMessageEvent = new AutoResetEvent(false);
        }
        #endregion
        
        #region Public Methods
        public void Start()
        {
            if (IsStarted) return;
            StopServer = new CancellationTokenSource();
            try
            {
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Any, SERVER_PORT));
                listener.Listen(10);
                listenerThread = new Thread(() => AcceptNewConnections(StopServer.Token));
                listenerThread.Start();
                _isStarted = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _isStarted = false;
            }
        }

        public void Stop()
        {
            StopServer.Cancel();
            listener.Close();
            connectionsMutex.WaitOne();
            foreach (var socket in ConnectedEsps) socket.Close();
            connectionsMutex.ReleaseMutex();            
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
        private void AcceptNewConnections(CancellationToken token)
        {
            while (token.IsCancellationRequested is false)
            {
                EspConnectedEvent.Reset();
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                EspConnectedEvent.Wait();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Await.Warning", "CS4014:Await.Warning")]
        private void AcceptCallback(IAsyncResult ar)
        {
            Socket socket= listener.EndAccept(ar);
            connectionsMutex.WaitOne();
            ConnectedEsps.Add(socket);
            connectionsMutex.ReleaseMutex();
            EspConnectedEvent.Set();
            Task.Run(() => ServeEspClientAsync(socket));
        }

        private async Task ServeEspClientAsync(Socket socket)
        {
            ESP_Message message = null;
            ESP32_Device esp = null;

            while (StopServer.IsCancellationRequested is false && socket.Connected)
            {
                message = await ReceiveMessageAsync(socket);
                if (message is null) break;

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
                                if (esp.Active == false)
                                {
                                    ESPManager.SetDeviceStatus(esp.MAC, true);
                                    Logger.Log("New ESP32 connection\t\tx: " + esp?.X_Position + " y: " + esp?.Y_Position + "\r\n");
                                }
                                else
                                {
                                    Logger.Log("An ESP32 disconnected\t\tx: " + esp?.X_Position + " y: " + esp?.Y_Position + "\r\n");
                                    Logger.Log("An ESP32 reconnected\t\tx: " + esp?.X_Position + " y: " + esp?.Y_Position + "\r\n");
                                }
                            }
                            if (CanReceiveData is true)
                                if (Send(socket, new Ok_Message().ToBytes()) is false)
                                    break;
                        }
                        else
                            break;
                }
                else if (message is Data_Message && mode == ServerMode.DATACOLLECTION_MODE)
                {
                    EnqueueMessage(message);
                    Logger.Log("An ESP sent DEVICE_DATA\t\tx: " + esp?.X_Position + " y: " + esp?.Y_Position + "\r\n");
                }
            }

            socket.Close();

            connectionsMutex.WaitOne();
            ConnectedEsps.Remove(socket);
            connectionsMutex.ReleaseMutex();

            /*if (esp != null)
            {
                ESPManager.SetDeviceStatus(esp.MAC, false);
            }*/
        }

        private static async Task<ESP_Message> ReceiveMessageAsync(Socket socket)
        {
            ESP_Message ret = null;
            int headerCode = -1;
            byte[] result = null;


            try { result = await ReceiveAsync(socket, 1); }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
            if (result is null) return null;

            headerCode = result[0];
            try
            {
                switch (headerCode)
                {
                    case Ready_Message.READY_HEADER:

                        result = new byte[Ready_Message.PAYLOAD_LENGTH];
                        result = await ReceiveAsync(socket, Ready_Message.PAYLOAD_LENGTH);
                        if (result is null)
                            return null;

                        ret = new Ready_Message
                        {
                            Header = Ready_Message.READY_HEADER,
                            Payload = Encoding.ASCII.GetString(result, 0, Ready_Message.PAYLOAD_LENGTH)
                        };
                        break;

                    case Data_Message.DATA_HEADER:

                        int jsonLenght = -1;
                        result = await ReceiveAsync(socket, 2);
                        if (result is null)
                            return null;

                        jsonLenght = BitConverter.ToUInt16(result, 0);
                        
                        result = await ReceiveAsync(socket, jsonLenght);
                        if (result is null)
                            return null;

                        ret = new Data_Message
                        {
                            Header = Data_Message.DATA_HEADER,
                            Payload = Encoding.ASCII.GetString(result, 0, jsonLenght)
                        };
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
            return ret;
        }

        private static async Task<byte[]> ReceiveAsync(Socket socket,int nBytes)
        {
            var buff = new byte[nBytes];
            int leftBytes = nBytes;
            while (leftBytes > 0)
            {
                try
                {
                    var recvLen = await Task.Factory.FromAsync((callback, state) => socket.BeginReceive(buff, nBytes - leftBytes, leftBytes, SocketFlags.None, callback, state),
                                                                asyncRes =>
                                                                {
                                                                    try { return socket.EndReceive(asyncRes); }
                                                                    catch { return -1; }
                                                                },null);
                    if (recvLen == -1)
                        return null;
                    leftBytes -= recvLen;
                }
                catch { return null; }                
            }
            return buff;
        }

        private static bool Send(Socket socket, byte[] bytes)
        {
            try
            {
                socket.Send(bytes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        private void EnqueueMessage(ESP_Message message)
        {
            messagesQueueMutex.WaitOne();
            messagesQueue.Enqueue(message);
            messagesQueueMutex.ReleaseMutex();
            NewMessageEvent.Set();
        }

        private void SyncronizeClients(object state)
        {
            connectionsMutex.WaitOne();
            foreach (var esp in ConnectedEsps)
                if (Send(esp, new Timestamp_Message().ToBytes()) is false) esp.Close();
            connectionsMutex.ReleaseMutex();
        }
        #endregion
    }
}