// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging
{
    /// <summary>
    /// Represents valid message types for communication.
    /// </summary>
    public class MessageTypes
    {
        /// <summary>
        /// Scaffolding is completed. No more messages can be sent after this.
        /// </summary>
        public static readonly MessageType Scaffolding_Completed = new MessageType("scaffolding_completed", 0);

        /// <summary>
        /// Scaffolding did not complete but had to terminate.
        /// </summary>
        public static readonly MessageType Terminate = new MessageType("terminate", 0);
        /// <summary>
        /// Request message for getting project information.
        /// <see cref="ProjectModel.IProjectContext"/>
        /// </summary>
        public static readonly MessageType ProjectInfoRequest = new MessageType("project_info_request", 0);
        /// <summary>
        /// Response message containing project information.
        /// <see cref="ProjectModel.IProjectContext"/>
        /// </summary>
        public static readonly MessageType ProjectInfoResponse = new MessageType("project_info_response", 0);
        /// <summary>
        /// File System Change message containing information about a
        /// single file system change. <see cref="FileSystemChange.FileSystemChangeInformation"/>
        /// </summary>
        public static readonly MessageType FileSystemChangeInformation = new MessageType("file_system_change", 1);
    }

    public class MessageType
    {
        internal MessageType(string value, int minProtocolVersion)
        {
            Value = value;
            MinProtocolVersion = minProtocolVersion;
        }

        public string Value { get; }

        public int MinProtocolVersion { get; }
    }
}
