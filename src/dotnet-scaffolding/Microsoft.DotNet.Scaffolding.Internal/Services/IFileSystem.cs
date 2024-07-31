// Copyright (c) Microsoft Corporation. All rights reserved.
namespace Microsoft.DotNet.Scaffolding.Internal.Services;

///<summary>
/// A wrapper interface to be used for all file system related operations for easy unit testing.
/// Any component that does some file system operations should only talk to this interface but not directly
/// to System.IO implementations. Unit tests can then provide a mock implementation of
/// this interface for testing that component.
///</summary>
public interface IFileSystem
{
    bool FileExists(string path);

    bool DirectoryExists(string dirPath);

    void CreateDirectory(string dirPath);

    string ReadAllText(string filePath);

    string[] ReadAllLines(string filePath);

    void WriteAllText(string filePath, string content);

    void WriteAllLines(string filePath, string[] content);

    Stream OpenFileStream(string path, FileMode mode, FileAccess access, FileShare share);

    IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption);

    IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);

    void DeleteFile(string filePath);

    void CopyFile(string sourcePath, string destinationPath, bool overwrite);

    string GetTempPath();

    DateTime GetLastWriteTime(string filePath);

    string? GetFileVersion(string filePath);

    Version? GetAssemblyVersion(string filePath);

    void MakeFileWritable(string filePath);

    Task AddFileAsync(string filePath, Stream fileStream);
}
