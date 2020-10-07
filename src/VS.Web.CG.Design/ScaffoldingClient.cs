// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Design
{
    public class ScaffoldingClient : IDisposable, IMessageSender
    {
        private static readonly string HostId = typeof(ScaffoldingClient).GetTypeInfo().Assembly.GetName().Name;

        private int _port;
        private TcpClient _client;
        private BinaryWriter _writer;
        private BinaryReader _reader;

        public bool TerminateSessionRequested { get; set; }
        private bool disposedValue = false; // To detect redundant calls
        private readonly ILogger _logger;

        public ISet<IMessageHandler> MessageHandlers { get; private set; }

        public int CurrentProtocolVersion => 1;

        public static async Task<ScaffoldingClient> Connect(int port, ILogger logger)
        {
            TcpClient client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);

            return new ScaffoldingClient(client, logger, port);
        }

        public ScaffoldingClient(TcpClient client, ILogger logger, int port)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            _client = client;
            _logger = logger;
            _port = port;
            Init();
        }

        public void Init()
        {
            if (_client.Connected)
            {
                var stream = _client.GetStream();
                _writer = new BinaryWriter(stream);
                _reader = new BinaryReader(stream);
                return;
            }
            else
            {
                throw new InvalidOperationException(string.Format(Resources.ConnectToServerError, _port));
            }
        }
        
        [SuppressMessage("supressing re-throw exception", "CA2200")]
        public bool Send(Message message)
        {
            lock (_writer)
            {
                try
                {
                    if (message.MessageType == MessageTypes.Terminate.Value)
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

        public Message CreateMessage(MessageType messageType, object payload, int protocolVersion)
        {
            if (messageType == null)
            {
                throw new ArgumentNullException(nameof(messageType));
            }

            return new Message()
            {
                MessageType = messageType.Value,
                HostId = HostId,
                Payload = payload == null ? null : JToken.FromObject(payload),
                ProtocolVersion = protocolVersion
            };
        }

        [SuppressMessage("supressing re-throw exception", "CA2200")]
        public Message ReadMessage()
        {
            try
            {
                var rawMessage = _reader.ReadString();
                var message = JsonConvert.DeserializeObject<Message>(rawMessage);

                if (MessageHandlers != null)
                {
                    foreach (var handler in MessageHandlers)
                    {
                        if (handler.HandleMessage(this, message))
                        {
                            break;
                        }
                    }
                    // No handler could handle the message.
                }
                return message;
            }
            catch (Exception ex)
            {
                _logger.LogMessage(ex.Message);
                throw ex;
            }
        }

        public void AddHandler(IMessageHandler handler)
        {
            if (MessageHandlers == null)
            {
                MessageHandlers = new HashSet<IMessageHandler>();
            }
            if (!MessageHandlers.Contains(handler))
            {
                MessageHandlers.Add(handler);
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
