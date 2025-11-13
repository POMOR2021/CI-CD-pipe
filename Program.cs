using Amazon.S3;
using Amazon.Runtime;
using Microsoft.EntityFrameworkCore;
using WebApplication27.Data;
using WebApplication27.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure PostgreSQL Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    // Fallback to in-memory database for local development without PostgreSQL
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("ImageGalleryDb"));
}

// Configure Yandex Object Storage (S3-compatible)
var accessKey = builder.Configuration["YandexStorage:AccessKey"];
var secretKey = builder.Configuration["YandexStorage:SecretKey"];
var serviceUrl = builder.Configuration["YandexStorage:ServiceUrl"] ?? "https://storage.yandexcloud.net";

if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
{
    var credentials = new BasicAWSCredentials(accessKey, secretKey);
    var config = new AmazonS3Config
    {
        ServiceURL = serviceUrl,
        ForcePathStyle = true
    };
    
    builder.Services.AddSingleton<IAmazonS3>(new AmazonS3Client(credentials, config));
    builder.Services.AddScoped<IStorageService, YandexStorageService>();
}
else
{
    // Mock storage service for local development
    builder.Services.AddScoped<IStorageService, LocalStorageService>();
}

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
