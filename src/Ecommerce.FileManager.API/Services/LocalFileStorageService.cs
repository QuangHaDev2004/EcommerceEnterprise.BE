// [nqha][2026-11-23]: Triển khai lưu trữ tệp tin trên ổ cứng cục bộ và tối ưu nén ảnh WebP cùng sinh Thumbnail qua ImageSharp
using Ecommerce.FileManager.API.Common;
using Ecommerce.FileManager.API.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Ecommerce.FileManager.API.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadRootPath;

    public LocalFileStorageService(IWebHostEnvironment environment)
    {
        _uploadRootPath = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads");
        if (!Directory.Exists(_uploadRootPath))
        {
            Directory.CreateDirectory(_uploadRootPath);
        }
    }

    public async Task<FileUploadResult> SaveFileAsync(IFormFile file, string baseUrl, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var isImage = FileSecurityValidator.IsImageExtension(extension);

        var now = DateTime.UtcNow;
        var subDir = Path.Combine(now.Year.ToString(), now.Month.ToString("D2"));
        var physicalFolder = Path.Combine(_uploadRootPath, subDir);

        if (!Directory.Exists(physicalFolder))
        {
            Directory.CreateDirectory(physicalFolder);
        }

        var fileId = Guid.NewGuid();
        var result = new FileUploadResult
        {
            Id = fileId,
            OriginalName = file.FileName,
            IsImage = isImage,
            CreatedAt = now
        };

        if (isImage)
        {
            // Tối ưu hóa ảnh: Chuyển đổi sang WebP và sinh Thumbnails
            var mainFileName = $"{fileId}.webp";
            var thumbSmFileName = $"{fileId}_thumb_sm.webp";
            var thumbMdFileName = $"{fileId}_thumb_md.webp";

            var mainFilePath = Path.Combine(physicalFolder, mainFileName);
            var thumbSmPath = Path.Combine(physicalFolder, thumbSmFileName);
            var thumbMdPath = Path.Combine(physicalFolder, thumbMdFileName);

            using (var inputStream = file.OpenReadStream())
            using (var image = await Image.LoadAsync(inputStream, cancellationToken))
            {
                var webpEncoder = new WebpEncoder
                {
                    Quality = 80,
                    FileFormat = WebpFileFormatType.Lossy
                };

                // 1. Lưu ảnh chính chuẩn WebP
                await image.SaveAsync(mainFilePath, webpEncoder, cancellationToken);

                // 2. Sinh Thumbnail nhỏ 150x150 px
                using (var thumbSm = image.Clone(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = new Size(150, 150),
                    Mode = ResizeMode.Crop
                })))
                {
                    await thumbSm.SaveAsync(thumbSmPath, webpEncoder, cancellationToken);
                }

                // 3. Sinh Thumbnail vừa 500x500 px
                using (var thumbMd = image.Clone(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = new Size(500, 500),
                    Mode = ResizeMode.Crop
                })))
                {
                    await thumbMd.SaveAsync(thumbMdPath, webpEncoder, cancellationToken);
                }
            }

            var fileInfo = new FileInfo(mainFilePath);
            result.FileName = mainFileName;
            result.FileSize = fileInfo.Length;
            result.MimeType = "image/webp";
            result.Url = $"{baseUrl}/uploads/{now.Year}/{now.Month:D2}/{mainFileName}";
            result.ThumbnailSm = $"{baseUrl}/uploads/{now.Year}/{now.Month:D2}/{thumbSmFileName}";
            result.ThumbnailMd = $"{baseUrl}/uploads/{now.Year}/{now.Month:D2}/{thumbMdFileName}";
        }
        else
        {
            // Lưu trữ tệp tài liệu nguyên bản
            var fileName = $"{fileId}{extension}";
            var filePath = Path.Combine(physicalFolder, fileName);

            using (var outputStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(outputStream, cancellationToken);
            }

            var fileInfo = new FileInfo(filePath);
            result.FileName = fileName;
            result.FileSize = fileInfo.Length;
            result.MimeType = file.ContentType;
            result.Url = $"{baseUrl}/uploads/{now.Year}/{now.Month:D2}/{fileName}";
        }

        return result;
    }

    public async Task<List<FileUploadResult>> SaveMultipleFilesAsync(IList<IFormFile> files, string baseUrl, CancellationToken cancellationToken = default)
    {
        var results = new List<FileUploadResult>();
        foreach (var file in files)
        {
            var result = await SaveFileAsync(file, baseUrl, cancellationToken);
            results.Add(result);
        }
        return results;
    }

    public Task<bool> DeleteFileAsync(string relativePath)
    {
        // relativePath ví dụ: "uploads/2026/11/guid.webp"
        var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        if (normalizedPath.StartsWith("uploads" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath = normalizedPath.Substring("uploads".Length + 1);
        }

        var fullPath = Path.Combine(_uploadRootPath, normalizedPath);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult(false);
        }

        File.Delete(fullPath);

        // Xóa cả các thumbnails liên quan nếu là ảnh
        var dir = Path.GetDirectoryName(fullPath) ?? "";
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fullPath);
        var thumbSm = Path.Combine(dir, $"{fileNameWithoutExt}_thumb_sm.webp");
        var thumbMd = Path.Combine(dir, $"{fileNameWithoutExt}_thumb_md.webp");

        if (File.Exists(thumbSm)) File.Delete(thumbSm);
        if (File.Exists(thumbMd)) File.Delete(thumbMd);

        return Task.FromResult(true);
    }
}
