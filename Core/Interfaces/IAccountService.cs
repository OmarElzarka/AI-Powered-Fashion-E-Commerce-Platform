using System.Security.Claims;
using Core.DTOs;
using Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace Core.Interfaces;

public interface IAccountService
{
    Task<IdentityResult> RegisterAsync(RegisterDto registerDto);
    Task<TokenDto?> LoginJwtAsync(LoginDto loginDto);
    Task<TokenDto?> RefreshTokenAsync(string token);
    Task<bool> CheckEmailExistsAsync(string email);
    Task LogoutAsync();
    Task<object?> GetUserInfoAsync(ClaimsPrincipal user);
    Task<(IdentityResult Result, AddressDto? Address)> CreateOrUpdateAddressAsync(ClaimsPrincipal user, AddressDto addressDto);
    Task<IdentityResult> UpdateProfileAsync(ClaimsPrincipal user, UpdateProfileDto profileDto);
    Task<IdentityResult> UpdateLanguageAsync(ClaimsPrincipal user, UpdateLanguageDto languageDto);
    Task<IdentityResult> DeleteAccountAsync(ClaimsPrincipal user);
}
