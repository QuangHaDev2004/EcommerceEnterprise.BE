// [nqha][2026-11-23]: Thực thể User khởi tạo hỗ trợ xác thực và phân quyền RBAC
using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities;

public class User : BaseEntity, IAggregateRoot
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Avatar { get; set; }
    public string Role { get; set; } = "Customer"; // Admin, Staff, Customer
    public bool IsActive { get; set; } = true;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
}
