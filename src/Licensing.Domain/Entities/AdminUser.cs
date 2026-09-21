using Licensing.Domain.Common;

namespace Licensing.Domain.Entities;

public class AdminUser : EntityBase
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Admin";
    public bool IsActive { get; set; } = true;
}
