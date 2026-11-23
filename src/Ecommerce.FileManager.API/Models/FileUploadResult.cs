// [nqha][2026-11-23]: DTO chứa kết quả phản hồi chi tiết sau khi upload file và sinh thumbnail thành công
namespace Ecommerce.FileManager.API.Models;

public class FileUploadResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OriginalName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailSm { get; set; }
    public string? ThumbnailMd { get; set; }
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public bool IsImage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
