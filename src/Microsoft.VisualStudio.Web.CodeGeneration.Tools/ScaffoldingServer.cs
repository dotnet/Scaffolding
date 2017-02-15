using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils.Messaging;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Tools
{
    internal class ScaffoldingServer : IDisposable, IMessageSender
    {
        private TcpListener _server;
        private Socket _socket;
        private BinaryWriter _writer;
        private BinaryReader _reader;
        private ILogger _logger;
        public static ScaffoldingServer Listen(ILogger logger)
        {
            TcpListener server = new TcpListener(new IPEndPoint(IPAddress.Loopback, 0));
            server.Start();

            return new ScaffoldingServer(server, logger);
        }

        internal ScaffoldingServer(TcpListener server, ILogger logger)
        {
            _server = server;
            Port = ((IPEndPoint)_server.LocalEndpoint).Port;
            _logger = logger;
        }

        public int Port { get; }
        public bool TerminateSessionRequested { get; private set; }
        public event EventHandler<Message> MessageReceived;
        public void Accept()
        {
            new Thread(async () =>
            {
                _socket = await _server.AcceptSocketAsync();

                var stream = new NetworkStream(_socket);
                _writer = new BinaryWriter(stream);
                _reader = new BinaryReader(stream);

                // Read incoming messages on the background thread
                ReadMessages();
            })
            { IsBackground = true }.Start();
        }

        public bool Send(Message message)
        {
            lock (_writer)
            {
                try
                {
                    if (message.MessageType == MessageTypes.Terminate)
                    {
                        TerminateSessionRequested = true;
                    }
                    _writer.Write(JsonConvert.SerializeObject(message));
                }
                catch (Exception ex)
                {
                    _logger.LogMessage(ex.Message, LogMessageLevel.Error);
                    throw;
                }
            }
            return true;
        }

        private void ReadMessages()
        {
            while (true)
            {
                try
                {
                    var rawMessage = _reader.ReadString();
                    var message = JsonConvert.DeserializeObject<Message>(rawMessage);
                    if (ShouldStopListening(message))
                    {
                        break;
                    }

                    MessageReceived?.Invoke(this, message);
                }
                catch (Exception ex)
                {
                    _logger.LogMessage(ex.Message, LogMessageLevel.Warning);
                    if (!TerminateSessionRequested)
                    {
                        continue;
                    }
                    break;
                }
            }
        }

        private bool ShouldStopListening(Message message)
        {
            if (message.MessageType == MessageTypes.Scaffolding_Completed)
            {
                return true;
            }

            return false;
        }

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _writer.Dispose();
                    _reader.Dispose();
                    _socket.Dispose();
                }

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
    }
}