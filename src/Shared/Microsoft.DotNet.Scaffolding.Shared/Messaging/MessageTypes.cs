// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.Shared.Messaging
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
        /// single file system change. <see cref="FileSystemChangeInformation"/>
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
