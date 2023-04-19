// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;

namespace Microsoft.DotNet.Scaffolding.Shared.Messaging
{
    public class FileSystemChangeMessageHandler : MessageHandlerBase
    {
        private static HashSet<string> _messagetypes = new HashSet<string>() { MessageTypes.FileSystemChangeInformation.Value };

        public FileSystemChangeMessageHandler(ILogger logger)
            : base(logger)
        {

        }

        public override ISet<string> MessageTypesHandled => _messagetypes;

        protected override bool HandleMessageInternal(IMessageSender sender, Message message)
        {
            // What if the deserialization fails?
            FileSystemChangeInformation info = message.Payload.ToObject<FileSystemChangeInformation>();
            if (info == null)
            {
                Logger.LogMessage(MessageStrings.InvalidFileSystemChangeMessage);
                Logger.LogMessage(message.Payload.ToString());
            }

            Logger.LogMessage($"{Environment.NewLine}\t\t{MessageStrings.StartFileSystemChangeToken}");
            switch (info.FileSystemChangeType)
            {
                case FileSystemChangeType.AddFile:
                    Logger.LogMessage(string.Format(MessageStrings.AddFileMessage, info.FullPath));
                    Logger.LogMessage(string.Format(MessageStrings.ContentsMessage, info.FileContents));
                    break;
                case FileSystemChangeType.EditFile:
                    Logger.LogMessage(string.Format(MessageStrings.EditFileMessage, info.FullPath));
                    Logger.LogMessage(string.Format(MessageStrings.ContentsMessage, info.FileContents));
                    break;
                case FileSystemChangeType.DeleteFile:
                    Logger.LogMessage(string.Format(MessageStrings.DeleteFileMessage, info.FullPath));
                    break;
                case FileSystemChangeType.AddDirectory:
                    Logger.LogMessage(string.Format(MessageStrings.AddDirectoryMessage, info.FullPath));
                    break;
                case FileSystemChangeType.RemoveDirectory:
                    Logger.LogMessage(string.Format(MessageStrings.RemoveDirectoryMessage, info.FullPath));
                    break;
            }
            Logger.LogMessage($"\t\t{MessageStrings.EndFileSystemChangeToken}{Environment.NewLine}");
            return true;
        }
    }
}
