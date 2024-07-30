// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Diagnostics;
using System.Reflection;

namespace Microsoft.DotNet.Scaffolding.Internal.Services;

/// <summary>
/// The default implementation of <see cref="IFileSystem"/>
/// used by product code. This just makes calls to methods in System.IO
/// </summary>
public class FileSystem : IFileSystem
{
    private static IFileSystem? _fileSystem;
    public static IFileSystem Instance => _fileSystem ??= new FileSystem();

    /// <inheritdoc />
    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    /// <inheritdoc />
    public bool DirectoryExists(string dirPath)
    {
        return Directory.Exists(dirPath);
    }

    /// <inheritdoc />
    public void CreateDirectory(string dirPath)
    {
        Directory.CreateDirectory(dirPath);
    }

    /// <inheritdoc />
    public string ReadAllText(string filePath)
    {
        return File.ReadAllText(filePath);
    }

    /// <inheritdoc />
    public void WriteAllText(string filePath, string content)
    {
        File.WriteAllText(filePath, content);
    }

    /// <inheritdoc />
    public string[] ReadAllLines(string filePath)
    {
        return File.ReadAllLines(filePath);
    }

    /// <inheritdoc />
    public void WriteAllLines(string filePath, string[] content)
    {
        File.WriteAllLines(filePath, content);
    }

    /// <inheritdoc />
    public Stream OpenFileStream(string path, FileMode mode, FileAccess access, FileShare share)
    {
        return new FileStream(path, mode, access, share);
    }

    /// <inheritdoc />
    public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption)
    {
        return Directory.EnumerateDirectories(path, searchPattern, searchOption);
    }

    /// <inheritdoc />
    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
    {
        return Directory.EnumerateFiles(path, searchPattern, searchOption);
    }

    /// <inheritdoc />
    public void DeleteFile(string filePath)
    {
        File.Delete(filePath);
    }

    /// <inheritdoc />
    public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
    {
        File.Copy(sourcePath, destinationPath, overwrite);
    }

    /// <inheritdoc />
    public string GetTempPath()
    {
        return Path.GetTempPath();
    }

    /// <inheritdoc />
    public DateTime GetLastWriteTime(string filePath)
    {
        return File.GetLastWriteTime(filePath);
    }

    /// <inheritdoc />
    public string? GetFileVersion(string filePath)
    {
        return FileVersionInfo.GetVersionInfo(filePath)?.FileVersion;
    }

    /// <inheritdoc />
    public Version? GetAssemblyVersion(string filePath)
    {
        return AssemblyName.GetAssemblyName(filePath)?.Version;
    }

    public void MakeFileWritable(string path)
    {
        Debug.Assert(File.Exists(path));

        FileAttributes attributes = File.GetAttributes(path);
        if (attributes.HasFlag(FileAttributes.ReadOnly))
        {
            File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
        }
    }

    public async Task AddFileAsync(string outputPath, Stream sourceStream)
    {
        using (var writeStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
        {
            await sourceStream.CopyToAsync(writeStream);
        }
    }
}
