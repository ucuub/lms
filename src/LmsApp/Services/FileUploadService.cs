namespace LmsApp.Services;

public interface IFileUploadService
{
    Task<string> UploadAsync(IFormFile file, string folder);
    void Delete(string filePath);
    bool IsValidFile(IFormFile file, string[] allowedExtensions, long maxSizeBytes);
}

public class FileUploadService(IWebHostEnvironment env, ILogger<FileUploadService> logger) : IFileUploadService
{
    private static readonly string[] DefaultAllowed = [".pdf", ".doc", ".docx", ".zip", ".png", ".jpg", ".jpeg"];
    private const long DefaultMaxSize = 10 * 1024 * 1024; // 10 MB

    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        // WebRootPath bisa null jika wwwroot tidak dikonfigurasi — fallback ke ContentRoot
        var webRoot = string.IsNullOrEmpty(env.WebRootPath)
            ? Path.Combine(env.ContentRootPath, "wwwroot")
            : env.WebRootPath;

        var uploadPath = Path.Combine(webRoot, "uploads", folder);
        Directory.CreateDirectory(uploadPath);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadPath, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        logger.LogInformation("File uploaded: {Path}", fullPath);
        return $"/uploads/{folder}/{fileName}";
    }

    public void Delete(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;
        var webRoot = string.IsNullOrEmpty(env.WebRootPath)
            ? Path.Combine(env.ContentRootPath, "wwwroot")
            : env.WebRootPath;
        var fullPath = Path.GetFullPath(Path.Combine(webRoot, filePath.TrimStart('/')));

        // Guard: pastikan path tetap di dalam webroot (cegah path traversal)
        if (!fullPath.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Delete blocked — path outside webroot: {Path}", fullPath);
            return;
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            logger.LogInformation("File deleted: {Path}", fullPath);
        }
    }

    public bool IsValidFile(IFormFile file, string[]? allowedExtensions = null, long maxSizeBytes = DefaultMaxSize)
    {
        allowedExtensions ??= DefaultAllowed;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(ext) && file.Length <= maxSizeBytes;
    }
}
