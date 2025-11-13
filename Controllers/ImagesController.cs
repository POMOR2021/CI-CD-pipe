using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication27.Data;
using WebApplication27.Models;
using WebApplication27.Services;

namespace WebApplication27.Controllers;

public class ImagesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IStorageService _storageService;
    private readonly ILogger<ImagesController> _logger;
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public ImagesController(
        ApplicationDbContext context,
        IStorageService storageService,
        ILogger<ImagesController> logger)
    {
        _context = context;
        _storageService = storageService;
        _logger = logger;
    }

    // GET: Images
    public async Task<IActionResult> Index()
    {
        var images = await _context.Images
            .OrderByDescending(i => i.UploadedAt)
            .ToListAsync();
        return View(images);
    }

    // GET: Images/Upload
    public IActionResult Upload()
    {
        return View();
    }

    // POST: Images/Upload
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file, string? description)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("", "Пожалуйста, выберите файл для загрузки");
            return View();
        }

        // Validate file size
        if (file.Length > MaxFileSize)
        {
            ModelState.AddModelError("", $"Размер файла не должен превышать {MaxFileSize / 1024 / 1024} MB");
            return View();
        }

        // Validate file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            ModelState.AddModelError("", "Разрешены только изображения форматов: JPG, JPEG, PNG, GIF, WEBP");
            return View();
        }

        try
        {
            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";

            // Upload to storage
            string storageUrl;
            using (var stream = file.OpenReadStream())
            {
                storageUrl = await _storageService.UploadFileAsync(stream, uniqueFileName, file.ContentType);
            }

            // Save metadata to database
            var imageMetadata = new ImageMetadata
            {
                FileName = uniqueFileName,
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                StorageUrl = storageUrl,
                UploadedAt = DateTime.UtcNow,
                Description = description
            };

            _context.Images.Add(imageMetadata);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Image {FileName} uploaded successfully", uniqueFileName);
            TempData["SuccessMessage"] = "Изображение успешно загружено!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image");
            ModelState.AddModelError("", "Произошла ошибка при загрузке изображения. Пожалуйста, попробуйте снова.");
            return View();
        }
    }

    // GET: Images/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var image = await _context.Images
            .FirstOrDefaultAsync(m => m.Id == id);
        
        if (image == null)
        {
            return NotFound();
        }

        return View(image);
    }

    // POST: Images/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var image = await _context.Images.FindAsync(id);
        if (image == null)
        {
            return NotFound();
        }

        try
        {
            // Delete from storage
            await _storageService.DeleteFileAsync(image.FileName);

            // Delete from database
            _context.Images.Remove(image);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Image {FileName} deleted successfully", image.FileName);
            TempData["SuccessMessage"] = "Изображение успешно удалено!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {FileName}", image.FileName);
            TempData["ErrorMessage"] = "Произошла ошибка при удалении изображения.";
        }

        return RedirectToAction(nameof(Index));
    }
}
