// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;

namespace Microsoft.DotNet.Scaffolding.Internal.Services;

/// <summary>
/// Copied this class from UpgradeAssistant project. 
/// </summary>
internal static class MacAddressGetter
{
    private const string InvalidMacAddress = "00-00-00-00-00-00";
    private const string MacRegex = @"(?:[a-z0-9]{2}[:\-]){5}[a-z0-9]{2}";
    private const string ZeroRegex = @"(?:00[:\-]){5}00";
    private const int ErrorFileNotFound = 0x2;

    public static async Task<string?> GetMacAddressAsync()
    {
        try
        {
            string? macAddress = await GetMacAddressCoreAsync();
            if (string.IsNullOrWhiteSpace(macAddress) || macAddress!.Equals(InvalidMacAddress, StringComparison.OrdinalIgnoreCase))
            {
                return GetMacAddressByNetworkInterface();
            }
            else
            {
                return macAddress;
            }
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> GetMacAddressCoreAsync()
    {
        try
        {
            string? shelloutput = await GetShellOutMacAddressOutputAsync();
            if (string.IsNullOrWhiteSpace(shelloutput))
            {
                return null;
            }

            return ParseMACAddress(shelloutput!);
        }
        catch (Win32Exception e)
        {
            if (e.NativeErrorCode == ErrorFileNotFound)
            {
                return GetMacAddressByNetworkInterface();
            }
            else
            {
                throw;
            }
        }
    }

    private static string? ParseMACAddress(string shelloutput)
    {
        foreach (Match match in Regex.Matches(shelloutput, MacRegex, RegexOptions.IgnoreCase))
        {
            if (!Regex.IsMatch(match.Value, ZeroRegex))
            {
                return match.Value;
            }
        }

        return null;
    }

    private static async Task<string?> GetIpCommandOutputAsync()
    {
        (int ipResult, string? ipStdOut, _) = await DotnetCliRunner.Create("ip", ["link"]).ExecuteAndCaptureOutputAsync();
        if (ipResult == 0)
        {
            return ipStdOut;
        }
        else
        {
            return null;
        }
    }

    private static async Task<string?> GetShellOutMacAddressOutputAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            (int result, string? stdOut, _) = await DotnetCliRunner.Create("getmac.exe", []).ExecuteAndCaptureOutputAsync();
            if (result == 0)
            {
                return stdOut;
            }
            else
            {
                return null;
            }
        }
        else
        {
            try
            {
                (int ifconfigResult, string? ifconfigStdOut, string? ifconfigStdErr) = await DotnetCliRunner.Create("ifconfig", ["-a"]).ExecuteAndCaptureOutputAsync();
                if (ifconfigResult == 0)
                {
                    return ifconfigStdOut;
                }
                else
                {
                    return await GetIpCommandOutputAsync();
                }
            }
            catch (Win32Exception e)
            {
                if (e.NativeErrorCode == ErrorFileNotFound)
                {
                    return await GetIpCommandOutputAsync();
                }
                else
                {
                    throw;
                }
            }
        }
    }

    private static string? GetMacAddressByNetworkInterface()
    {
        return GetMacAddressesByNetworkInterface().Where(x => !x.Equals(InvalidMacAddress, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
    }

    private static List<string> GetMacAddressesByNetworkInterface()
    {
        var nics = NetworkInterface.GetAllNetworkInterfaces();
        var macs = new List<string>();

        if (nics == null || nics.Length < 1)
        {
            macs.Add(string.Empty);
            return macs;
        }

        foreach (NetworkInterface adapter in nics)
        {
            IPInterfaceProperties properties = adapter.GetIPProperties();

            PhysicalAddress address = adapter.GetPhysicalAddress();
            byte[] bytes = address.GetAddressBytes();
#pragma warning disable CA1305 // Specify IFormatProvider
            macs.Add(string.Join("-", bytes.Select(x => x.ToString("X2"))));
#pragma warning restore CA1305 // Specify IFormatProvider
            if (macs.Count >= 10)
            {
                break;
            }
        }

        return macs;
    }
}
