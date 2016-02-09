// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.CodeGeneration.DotNet
{
    public interface IApplicationEnvironment
    {
        string ApplicationBasePath { get; }
        string ApplicationName { get; }
    }
}
