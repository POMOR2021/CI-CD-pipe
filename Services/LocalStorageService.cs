namespace WebApplication27.Services;

public class LocalStorageService : IStorageService
{
    private readonly string _storagePath;
    private readonly ILogger<LocalStorageService> _logger;
    private readonly IWebHostEnvironment _environment;

    public LocalStorageService(
        IWebHostEnvironment environment,
        ILogger<LocalStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
        _storagePath = Path.Combine(environment.WebRootPath, "uploads");
        
        // Create uploads directory if it doesn't exist
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        try
        {
            var filePath = Path.Combine(_storagePath, fileName);
            
            using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileStreamOutput);
            }

            _logger.LogInformation("File {FileName} uploaded successfully to local storage", fileName);
            return GetFileUrl(fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName} to local storage", fileName);
            throw;
        }
    }

    public Task<bool> DeleteFileAsync(string fileName)
    {
        try
        {
            var filePath = Path.Combine(_storagePath, fileName);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("File {FileName} deleted successfully from local storage", fileName);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileName} from local storage", fileName);
            return Task.FromResult(false);
        }
    }

    public string GetFileUrl(string fileName)
    {
        return $"/uploads/{fileName}";
    }
}
