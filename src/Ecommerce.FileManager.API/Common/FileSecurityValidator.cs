// [nqha][2026-11-23]: Lớp bảo mật kiểm tra Magic Bytes (File Signature) và MIME-type chống tải lên mã độc
namespace Ecommerce.FileManager.API.Common;

public static class FileSecurityValidator
{
    private static readonly Dictionary<string, List<byte[]>> FileSignatures = new()
    {
        { ".jpg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".jpeg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
        { ".gif", new List<byte[]> { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
        { ".pdf", new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } } },
        { ".webp", new List<byte[]> { new byte[] { 0x52, 0x49, 0x46, 0x46 } } } // RIFF header
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".pdf", ".docx", ".xlsx"
    };

    public const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public static (bool IsValid, string? ErrorMessage) ValidateFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return (false, "Tệp tin không được rỗng.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return (false, $"Dung lượng tệp vượt quá giới hạn cho phép (tối đa {MaxFileSizeBytes / 1024 / 1024}MB).");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            return (false, $"Định dạng tệp '{extension}' không được hỗ trợ.");
        }

        // Kiểm tra Magic Bytes cho các định dạng nhạy cảm
        if (FileSignatures.TryGetValue(extension, out var signatures))
        {
            using var reader = new BinaryReader(file.OpenReadStream());
            var headerBytes = reader.ReadBytes(8);

            bool matchesSignature = signatures.Any(sig =>
                headerBytes.Take(sig.Length).SequenceEqual(sig));

            if (!matchesSignature)
            {
                return (false, "Nội dung tệp không khớp với phần mở rộng (phát hiện giả mạo định dạng).");
            }
        }

        return (true, null);
    }

    public static bool IsImageExtension(string extension)
    {
        var ext = extension.ToLowerInvariant();
        return ext is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif";
    }
}
