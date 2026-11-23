// [nqha][2026-11-23]: Interface dịch vụ lưu trữ và xử lý đa phương tiện cho File Storage Microservice
using Ecommerce.FileManager.API.Models;

namespace Ecommerce.FileManager.API.Services;

public interface IFileStorageService
{
    Task<FileUploadResult> SaveFileAsync(IFormFile file, string baseUrl, CancellationToken cancellationToken = default);
    Task<List<FileUploadResult>> SaveMultipleFilesAsync(IList<IFormFile> files, string baseUrl, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string relativePath);
}
