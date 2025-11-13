namespace WebApplication27.Services;

public interface IStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    Task<bool> DeleteFileAsync(string fileName);
    string GetFileUrl(string fileName);
}
