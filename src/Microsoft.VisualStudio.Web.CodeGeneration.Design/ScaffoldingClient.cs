using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils.Messaging;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Design
{
    public class ScaffoldingClient : IDisposable, IMessageSender
    {
        private TcpClient _client;
        private BinaryWriter _writer;
        private BinaryReader _reader;

        public bool TerminateSessionRequested { get; set; }
        private bool disposedValue = false; // To detect redundant calls
        private readonly ILogger _logger;

        public event EventHandler<Message> MessageReceived;

        public static ScaffoldingClient Connect(int port, ILogger logger)
        {
            TcpClient client = new TcpClient();
            client.ConnectAsync(IPAddress.Loopback, port);

            return new ScaffoldingClient(client, logger);
        }

        public ScaffoldingClient(TcpClient client, ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            _client = client;
            _logger = logger;
            Init();
        }

        public void Init()
        {
            var stream = _client.GetStream();
            _writer = new BinaryWriter(stream);
            _reader = new BinaryReader(stream);
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
                    throw ex;
                }
            }

            return true;
        }

        public void ReadMessage()
        {
            try
            {
                var rawMessage = _reader.ReadString();
                var message = JsonConvert.DeserializeObject<Message>(rawMessage);

                MessageReceived?.Invoke(this, message);
            }
            catch (Exception ex)
            {
                _logger.LogMessage(ex.Message);
                throw ex;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _writer.Dispose();
                    _reader.Dispose();
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
