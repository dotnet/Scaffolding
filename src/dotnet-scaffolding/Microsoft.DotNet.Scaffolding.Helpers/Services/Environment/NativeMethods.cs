// Copyright (c) Microsoft Corporation. All rights reserved.
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Setup.Configuration;

namespace Microsoft.DotNet.Scaffolding.Helpers.Environment;

#pragma warning disable CA5392 // Use DefaultDllImportSearchPaths attribute for P/Invokes
#pragma warning disable SA1114 // Parameter list should follow declaration

internal static class NativeMethods
{
    [DllImport("Microsoft.VisualStudio.Setup.Configuration.Native.dll", ExactSpelling = true, PreserveSig = true)]
    public static extern int GetSetupConfiguration(
        [MarshalAs(UnmanagedType.Interface)][Out] out ISetupConfiguration configuration,
        IntPtr reserved);
}
