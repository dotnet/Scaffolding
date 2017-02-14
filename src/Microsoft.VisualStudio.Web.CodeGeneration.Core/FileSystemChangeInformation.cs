namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public class FileSystemChangeInformation
    {
        public string FullPath { get; set; }
        public ChangeType ChangeType { get; set; }
        public string FileContents { get; set; }
    }

    public enum ChangeType
    {
        AddFile,
        EditFile,
        DeleteFile,
        AddDirectory,
        RemoveDirectory
    }
}