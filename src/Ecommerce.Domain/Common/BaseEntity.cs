// [nqha][2026-11-23]: BaseEntity trừu tượng chứa các thuộc tính cốt lõi (Id, Ngày tạo, Ngày cập nhật, Soft delete)
namespace Ecommerce.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}
