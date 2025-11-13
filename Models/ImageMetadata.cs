namespace WebApplication27.Models;

public class ImageMetadata
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StorageUrl { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string? Description { get; set; }
}
