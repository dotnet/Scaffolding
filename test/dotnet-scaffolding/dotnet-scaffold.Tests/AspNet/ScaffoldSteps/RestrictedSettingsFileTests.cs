// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class RestrictedSettingsFileTests
{
    [SkippableTheory]
    [InlineData(0)]
    [InlineData(256)] // 0400
    [InlineData(384)] // 0600
    [InlineData(416)] // 0640
    [UnsupportedOSPlatform("windows")]
    public void AtomicReplacement_PreservesUnixMode(int mode)
    {
        Skip.If(OperatingSystem.IsWindows(), "Unix file modes require Unix.");
        string directory = CreateDirectory();
        string destination = Path.Combine(directory, "appsettings.Development.json");
        string source = Path.Combine(directory, "settings.new");
        try
        {
            File.WriteAllText(destination, "original secret");
            File.SetUnixFileMode(destination, (UnixFileMode)mode);
            var fileSystem = new FileSystem();
            fileSystem.WriteAllTextRestricted(source, "preserved secret and updated settings");
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(source));

            fileSystem.ReplaceFilePreservingPermissions(source, destination, overwrite: true);

            Assert.Equal((UnixFileMode)mode, File.GetUnixFileMode(destination));
            Assert.False(File.Exists(source));
            File.SetUnixFileMode(destination, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            Assert.Equal("preserved secret and updated settings", File.ReadAllText(destination));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [SkippableFact]
    [UnsupportedOSPlatform("windows")]
    public void NewSettings_AreOwnerOnlyOnUnix()
    {
        Skip.If(OperatingSystem.IsWindows(), "Unix file modes require Unix.");
        string directory = CreateDirectory();
        try
        {
            var fileSystem = new FileSystem();
            string source = Path.Combine(directory, "settings.new");
            string destination = Path.Combine(directory, "appsettings.Development.json");
            fileSystem.WriteAllTextRestricted(source, "secret");
            fileSystem.ReplaceFilePreservingPermissions(source, destination, overwrite: true);
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(destination));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [SkippableFact]
    [SupportedOSPlatform("windows")]
    public void AtomicReplacement_PreservesExplicitWindowsDacl()
    {
        Skip.IfNot(OperatingSystem.IsWindows(), "Windows DACLs require Windows.");
        string directory = CreateDirectory();
        try
        {
            string destination = Path.Combine(directory, "appsettings.Development.json");
            string source = Path.Combine(directory, "settings.new");
            File.WriteAllText(destination, "original secret");
            var security = new FileSecurity();
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            security.AddAccessRule(new FileSystemAccessRule(
                WindowsIdentity.GetCurrent().User!, FileSystemRights.Read | FileSystemRights.Write | FileSystemRights.Delete,
                AccessControlType.Allow));
            new FileInfo(destination).SetAccessControl(security);
            string originalDacl = new FileInfo(destination).GetAccessControl().GetSecurityDescriptorSddlForm(AccessControlSections.Access);
            var fileSystem = new FileSystem();
            fileSystem.WriteAllTextRestricted(source, "preserved secret and updated settings");
            FileSecurity sourceSecurity = new FileInfo(source).GetAccessControl();
            Assert.True(sourceSecurity.AreAccessRulesProtected);
            var rule = Assert.IsType<FileSystemAccessRule>(Assert.Single(sourceSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier))));
            Assert.Equal(WindowsIdentity.GetCurrent().User, rule.IdentityReference);
            Assert.Equal(AccessControlType.Allow, rule.AccessControlType);

            fileSystem.ReplaceFilePreservingPermissions(source, destination, overwrite: true);

            Assert.Equal(originalDacl, new FileInfo(destination).GetAccessControl().GetSecurityDescriptorSddlForm(AccessControlSections.Access));
            Assert.Equal("preserved secret and updated settings", File.ReadAllText(destination));
            Assert.False(File.Exists(source));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateDirectory()
    {
        string directory = Path.Combine(Directory.GetCurrentDirectory(), $"RestrictedSettings-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
