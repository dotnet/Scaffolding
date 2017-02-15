// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging
{
    public class MessageTypes
    {
        public const string Scaffolding_Completed = "scaffolding_completed";
        public const string Terminate = "terminate";
        public const string ProjectInfoRequest = "project_info_request";
        public const string ProjectInfoResponse = "project_info_response";
        public const string FileSystemChangeInformation = "file_system_change";
    }
}
