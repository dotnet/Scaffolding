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
        public const string Scaffolding_Completed = "scaffolding_completed";

        /// <summary>
        /// Scaffolding did not complete but had to terminate.
        /// </summary>
        public const string Terminate = "terminate";

        /// <summary>
        /// Request message for getting project information.
        /// <see cref="ProjectModel.IProjectContext"/>
        /// </summary>
        public const string ProjectInfoRequest = "project_info_request";

        /// <summary>
        /// Response message containing project information.
        /// <see cref="ProjectModel.IProjectContext"/>
        /// </summary>
        public const string ProjectInfoResponse = "project_info_response";

        /// <summary>
        /// File System Change message containing information about a
        /// single file system change. <see cref="FileSystemChange.FileSystemChangeInformation"/>
        /// </summary>
        public const string FileSystemChangeInformation = "file_system_change";
    }
}
