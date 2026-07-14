using System;

namespace Core.Entities;

public class RefreshToken : BaseEntity
{
    public required string Token { get; set; }
    public DateTime Expires { get; set; }
    public bool IsExpired => DateTime.UtcNow >= Expires;
    public DateTime Created { get; set; }
    public DateTime? Revoked { get; set; }
    public bool IsActive => Revoked == null && !IsExpired;

    // Navigation properties
    public required string AppUserId { get; set; }
    public AppUser? AppUser { get; set; }
}
