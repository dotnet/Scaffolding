// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Spectre.Console;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers
{
    internal static class SpectreExtensions
    {
        public static Status WithSpinner(this Status status)
        {
            return status
                .AutoRefresh(true)
                .Spinner(Spinner.Known.Aesthetic)
                .SpinnerStyle(Style.Parse("lightseagreen"));
        }
    }
}
