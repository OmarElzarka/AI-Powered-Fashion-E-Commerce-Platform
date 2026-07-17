using System;
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class RegisterDto
{
    [Required] public string FirstName { get; set; } = string.Empty;
    [Required] public string LastName { get; set; } = string.Empty;
    [Required] [EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] [MinLength(6)] public string Password { get; set; } = string.Empty;
    [Required] public string PhoneNumber { get; set; } = string.Empty;
    [Required] public string Country { get; set; } = string.Empty;
    [Required] public string City { get; set; } = string.Empty;
    [Required] public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    [Required] public string PostalCode { get; set; } = string.Empty;
}
