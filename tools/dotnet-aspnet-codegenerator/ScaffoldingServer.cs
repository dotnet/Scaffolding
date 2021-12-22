// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Tools
{
    internal class ScaffoldingServer : IDisposable, IMessageSender
    {
        private static readonly string HostId = typeof(ScaffoldingServer).GetTypeInfo().Assembly.GetName().Name;

        private TcpListener _server;
        private Socket _socket;
        private BinaryWriter _writer;
        private BinaryReader _reader;
        private ILogger _logger;
        private Thread _readerThread;

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

        public ISet<IMessageHandler> MessageHandlers { get; private set; }

        public void Accept()
        {
            _readerThread = new Thread(async () =>
            {
                _socket = await _server.AcceptSocketAsync();

                var stream = new NetworkStream(_socket);
                _writer = new BinaryWriter(stream);
                _reader = new BinaryReader(stream);

                // Read incoming messages on the background thread
                ReadMessages();
            })
            { IsBackground = true };
            
            _readerThread.Start();
        }

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
                    throw;
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
                Payload = payload == null ? null: JToken.FromObject(payload),
                ProtocolVersion = protocolVersion
            };
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

        public void WaitForExit(TimeSpan timeout)
        {
            if (_readerThread == null)
            {
                return;
            }
            _readerThread.Join(timeout.Milliseconds);
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
            if (message.MessageType == MessageTypes.Scaffolding_Completed.Value)
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
                    _server.Stop();
                    _writer?.Dispose();
                    _reader?.Dispose();
                    _socket?.Dispose();
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
