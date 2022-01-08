// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Threading.Tasks;
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

        private BinaryWriter _writer;
        private BinaryReader _reader;
        private ILogger _logger;

        public static ScaffoldingServer Listen(ILogger logger)
        {
            return new ScaffoldingServer(logger);
        }

        internal ScaffoldingServer(ILogger logger)
        {

            var stream = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            try {
                this._writer = new BinaryWriter(stream);
                this.Port[0X1] = stream.GetClientHandleAsString();
                stream = null;
            }
            finally { if (this._writer is null) using (stream) {} }

            stream = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            try {
                this._reader = new BinaryReader(stream);
                this.Port[0X0] = stream.GetClientHandleAsString();
                stream = null;
            }
            finally { if (this._reader is null) using (stream) {} }    
            this._logger = logger;
        }

        public string[] Port { get; } = new string[0X2];
        public bool TerminateSessionRequested { get; private set; }

        public ISet<IMessageHandler> MessageHandlers { get; private set; }

        public async Task Accept()
        {
            ((AnonymousPipeServerStream) this._reader.BaseStream).DisposeLocalCopyOfClientHandle();
            // Read incoming messages on the background thread
            await this.ReadMessages();
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

        private async Task ReadMessages()
        {
            for (;;)
            {
                try
                {
                    var rawMessage = await Task.Run(() => this._reader.ReadString());
                    var message = JsonConvert.DeserializeObject<Message>(rawMessage);
                    if (ShouldStopListening(message))
                    {
                        return;
                    }

                    if (this.MessageHandlers is IEnumerable<IMessageHandler>)
                    {
                        foreach (var handler in this.MessageHandlers)
                        {
                            if (handler.HandleMessage(this, message))
                            {
                                break;
                            }
                        }
                        // No handler could handle the message.
                    }
                }
                catch (EndOfStreamException)
                {
                    return;
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
                    _writer?.Dispose();
                    _reader?.Dispose();
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
