// [nqha][2026-11-23]: Controller tiếp nhận HTTP requests xử lý upload tệp đơn, upload hàng loạt và xóa tệp
using Ecommerce.FileManager.API.Common;
using Ecommerce.FileManager.API.Models;
using Ecommerce.FileManager.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.FileManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IFileStorageService fileStorageService, ILogger<FilesController> logger)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<FileUploadResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadSingleFile(IFormFile? file, CancellationToken cancellationToken)
    {
        var (isValid, errorMessage) = FileSecurityValidator.ValidateFile(file);
        if (!isValid)
        {
            return BadRequest(new { success = false, message = errorMessage });
        }

        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _fileStorageService.SaveFileAsync(file!, baseUrl, cancellationToken);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong quá trình xử lý tệp: {FileName}", file?.FileName);
            return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = "Không thể xử lý hoặc nén tệp tin." });
        }
    }

    [HttpPost("upload-multiple")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<List<FileUploadResult>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadMultipleFiles([FromForm] List<IFormFile>? files, CancellationToken cancellationToken)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest(new { success = false, message = "Danh sách tệp tin không được để trống." });
        }

        if (files.Count > 10)
        {
            return BadRequest(new { success = false, message = "Chỉ cho phép tải lên tối đa 10 tệp mỗi lần." });
        }

        foreach (var file in files)
        {
            var (isValid, errorMessage) = FileSecurityValidator.ValidateFile(file);
            if (!isValid)
            {
                return BadRequest(new { success = false, message = $"Tệp '{file.FileName}': {errorMessage}" });
            }
        }

        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var results = await _fileStorageService.SaveMultipleFilesAsync(files, baseUrl, cancellationToken);
            return Ok(new { success = true, data = results });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý tải lên nhiều tệp tin");
            return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = "Lỗi hệ thống khi lưu trữ nhiều tệp tin." });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteFile([FromQuery] string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return BadRequest(new { success = false, message = "Đường dẫn tệp tin không được để trống." });
        }

        var deleted = await _fileStorageService.DeleteFileAsync(path);
        if (!deleted)
        {
            return NotFound(new { success = false, message = "Không tìm thấy tệp tin cần xóa." });
        }

        return Ok(new { success = true, message = "Xóa tệp tin thành công." });
    }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
}
