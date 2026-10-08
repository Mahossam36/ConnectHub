namespace ConnectHub.BLL.Interfaces.Storage;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(
        Stream stream,
        string fileName,
        string folder);

    Task DeleteFileAsync(
        string relativePath);

    Task<Stream?> GetFileStreamAsync(
        string relativePath);
}
