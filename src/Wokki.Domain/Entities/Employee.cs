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
    /// <summary>URL ảnh enrollment khuôn mặt (Cloudinary) — chụp lần check-in đầu tiên.</summary>
    public string? FaceEnrollmentPhotoUrl { get; set; }
    /// <summary>Cloudinary public_id của ảnh enrollment — xóa/thay ảnh.</summary>
    public string? FaceEnrollmentPhotoPublicId { get; set; }
    /// <summary>face-api.js descriptor (JSON array string) dùng so khớp client-side ở các lần check-in sau.</summary>
    public string? FaceEmbedding { get; set; }
    public decimal HourlyRate { get; set; }
    public Guid? DepartmentId { get; set; }
    public DateTime EmployedAt { get; set; } = DateTime.UtcNow;
    public DateTime? TerminatedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
