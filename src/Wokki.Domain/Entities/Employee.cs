namespace Wokki.Domain.Entities;

public class Employee
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    /// <summary>STK ngân hàng — nhân viên tự cập nhật; Admin dùng khi chuyển lương.</summary>
    public string? BankAccountNumber { get; set; }
    /// <summary>Tên chủ tài khoản (khớp ngân hàng).</summary>
    public string? BankAccountHolderName { get; set; }
    /// <summary>Tên ngân hàng (vd. Vietcombank).</summary>
    public string? BankName { get; set; }
    /// <summary>URL ảnh QR chuyển khoản (Cloudinary).</summary>
    public string? PaymentQrImageUrl { get; set; }
    /// <summary>Cloudinary public_id — xóa/thay ảnh.</summary>
    public string? PaymentQrPublicId { get; set; }
    public decimal HourlyRate { get; set; }
    public Guid DepartmentId { get; set; }
    public DateTime EmployedAt { get; set; } = DateTime.UtcNow;
    public DateTime? TerminatedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
