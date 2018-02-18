using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Debug = UnityEngine.Debug;

namespace Assets.Sceelix.Communication
{
    public delegate void TcpClientEventHandler();
    public delegate void TcpClientRawMessageEventHandler(String message);
    public delegate void TcpClientParsedMessageEventHandler(JToken token);

    public class MessageClient
    {
        private readonly TcpClient _client;

        private readonly NetworkStream _clientConnection;
        private readonly StreamReader _clientStreamReader;
        private readonly StreamWriter _clientStreamWriter;

        private readonly Queue<Action> _actions = new Queue<Action>();
        private readonly Thread _processMessageThread;

        private readonly Timer _pingTimer;
        private int _pingPeriod;

        //delegates to inform of various connection events
        public event TcpClientEventHandler ClientDisconnected;
        public event TcpClientRawMessageEventHandler RawMessageReceived;
        public event TcpClientParsedMessageEventHandler ParsedMessageReceived;

    

        public MessageClient(string hostname, int port, int pingPeriod = 3000)
        {
            _client = new TcpClient(hostname, port);

            //Do(_client.Client);

            _pingPeriod = pingPeriod;

            _clientConnection = _client.GetStream();
            _clientStreamReader = new StreamReader(_clientConnection);
            _clientStreamWriter = new StreamWriter(_clientConnection);

            _processMessageThread = new Thread(ProcessMessages) {Name = "ProcessMessages",IsBackground = true};
            _processMessageThread.Start();

            _pingTimer = new Timer(state => OnPing(), null, 0, pingPeriod);
        }


        private void OnPing()
        {
            //TcpClient / NetworkStream does not get notified when the connection is closed.
            //IOControlCode.KeepAliveValues does not work in Unity
            //The only option available is to catch exceptions when writing to the stream.
            //so we send this message periodically (which should just be ignored on the other side) to figure 
            //out if the connection is active
            if(Connected)
                SendRawMessage(String.Empty);
        }



        private void ProcessMessages()
        {
            try
            {
                while (true)
                {
                    string readLine = _clientStreamReader.ReadLine();

                    //discard empty messages or pings here
                    if (!String.IsNullOrEmpty(readLine))
                    {
                        try
                        {
                            if (RawMessageReceived != null)
                            {
                                lock (_actions)
                                {
                                    _actions.Enqueue(() => RawMessageReceived(readLine));
                                }
                            }

                            if (ParsedMessageReceived != null)
                            {
                                lock (_actions)
                                {
                                    //var stopwatch = Stopwatch.StartNew();
                                
                                    JToken jToken = JToken.Parse(readLine);
                                    //UnityEngine.Debug.Log("Deserialization took: " + stopwatch.ElapsedMilliseconds);

                                    _actions.Enqueue(() => ParsedMessageReceived(jToken));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Log(ex);
                        }
                    }
                }
            }
                //when the connection is closed, this exception will be thrown, ending the threaded function
            catch (IOException)
            {
                if (ClientDisconnected != null)
                    ClientDisconnected();
            }
        }



        /// <summary>
        /// Executes the events in the main thread.
        /// </summary>
        public void Synchronize()
        {
            lock (_actions)
            {
                while (_actions.Count > 0)
                {
                    _actions.Dequeue().Invoke();
                }
            }
        }


        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="data"></param>
        public void SendMessage(String subject, Object data)
        {
            if (_client.Connected)
            {
                string message = JsonConvert.SerializeObject(new TcpMessage(subject, data), Formatting.None);

                _clientStreamWriter.WriteLine(message);
                _clientStreamWriter.Flush();
            }

        }


        public void SendRawMessage(String message)
        {
            try
            {
                if (_client.Connected)
                {
                    _clientStreamWriter.WriteLine(message);
                    _clientStreamWriter.Flush();
                }
            }
            catch (SocketException)
            {
                Disconnect();
            }
        }


        /// <summary>
        /// Closes the connection.
        /// </summary>
        public void Disconnect()
        {
            _clientStreamWriter.Close();
            _clientStreamReader.Close();
            _clientConnection.Close();
            _client.Close();
            _processMessageThread.Abort();
            _pingTimer.Dispose();
        
            if (ClientDisconnected != null)
                ClientDisconnected();
        }


        /// <summary>
        /// Indicates if the client is connected to the server.
        /// </summary>
        public bool Connected
        {
            get { return _client != null && _client.Connected; }
        }


        /// <summary>
        /// Time interval between server pings. 
        /// Required to understand if the connection is active. 
        /// Default is 3000ms.
        /// </summary>
        public int PingPeriod
        {
            get { return _pingPeriod; }
            set
            {
                if (value != _pingPeriod)
                {
                    _pingPeriod = value;

                    _pingTimer.Change(0, _pingPeriod);
                }
            }
        }
    }
}