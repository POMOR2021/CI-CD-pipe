using Amazon.S3;
using Amazon.S3.Model;

namespace WebApplication27.Services;

public class YandexStorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<YandexStorageService> _logger;

    public YandexStorageService(
        IAmazonS3 s3Client,
        IConfiguration configuration,
        ILogger<YandexStorageService> logger)
    {
        _s3Client = s3Client;
        _bucketName = configuration["YandexStorage:BucketName"] 
            ?? throw new ArgumentNullException("BucketName is not configured");
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName,
                InputStream = fileStream,
                ContentType = contentType,
                CannedACL = S3CannedACL.PublicRead
            };

            var response = await _s3Client.PutObjectAsync(request);
            
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                _logger.LogInformation("File {FileName} uploaded successfully", fileName);
                return GetFileUrl(fileName);
            }

            throw new Exception($"Failed to upload file. Status code: {response.HttpStatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName}", fileName);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string fileName)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName
            };

            var response = await _s3Client.DeleteObjectAsync(request);
            _logger.LogInformation("File {FileName} deleted successfully", fileName);
            return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileName}", fileName);
            return false;
        }
    }

    public string GetFileUrl(string fileName)
    {
        return $"https://storage.yandexcloud.net/{_bucketName}/{fileName}";
    }
}
