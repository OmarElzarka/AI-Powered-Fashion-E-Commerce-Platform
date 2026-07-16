using System.Security.Claims;
using Core.DTOs;
using Core.Entities;
using Core.Extensions;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class AccountService(UserManager<AppUser> userManager, ITokenService tokenService, StoreContext context) : IAccountService
{
    public async Task<IdentityResult> RegisterAsync(RegisterDto registerDto)
    {
        var user = new AppUser
        {
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            Email = registerDto.Email,
            UserName = registerDto.Email,
            PhoneNumber = registerDto.PhoneNumber,
            Address = new Address
            {
                Line1 = registerDto.Line1,
                Line2 = registerDto.Line2,
                City = registerDto.City,
                Country = registerDto.Country,
                PostalCode = registerDto.PostalCode
            }
        };

        return await userManager.CreateAsync(user, registerDto.Password);
    }

    public async Task<TokenDto?> LoginJwtAsync(LoginDto loginDto)
    {
        var user = await userManager.FindByEmailAsync(loginDto.Email);
        if (user == null) 
        {
            Console.WriteLine($"DEBUG: Login failed. User '{loginDto.Email}' not found.");
            return null;
        }

        var result = await userManager.CheckPasswordAsync(user, loginDto.Password) ? IdentityResult.Success : IdentityResult.Failed();
        if (!result.Succeeded) 
        {
            Console.WriteLine($"DEBUG: Login failed. CheckPasswordSignInAsync failed for user '{loginDto.Email}'. ");
            return null;
        }

        var accessToken = await tokenService.GenerateJwtToken(user);
        var refreshToken = await tokenService.GenerateRefreshToken(user);

        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        return new TokenDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token
        };
    }

    public async Task<TokenDto?> RefreshTokenAsync(string token)
    {
        var refreshToken = await context.RefreshTokens
            .Include(x => x.AppUser)
            .FirstOrDefaultAsync(x => x.Token == token);

        if (refreshToken == null || !refreshToken.IsActive || refreshToken.AppUser == null)
            return null;

        refreshToken.Revoked = DateTime.UtcNow;

        var newAccessToken = await tokenService.GenerateJwtToken(refreshToken.AppUser);
        var newRefreshToken = await tokenService.GenerateRefreshToken(refreshToken.AppUser);

        context.RefreshTokens.Add(newRefreshToken);
        await context.SaveChangesAsync();

        return new TokenDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token
        };
    }

    public async Task<bool> CheckEmailExistsAsync(string email)
    {
        return await userManager.FindByEmailAsync(email) != null;
    }

    public async Task LogoutAsync()
    {
        // No-op for JWT
    }

    public async Task<object?> GetUserInfoAsync(ClaimsPrincipal userPrincipal)
    {
        var user = await userManager.Users.Include(x => x.Address).FirstOrDefaultAsync(x => x.Email == userPrincipal.FindFirstValue(ClaimTypes.Email));
        if (user == null) return null;

        return new
        {
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.Language,
            Address = user.Address?.ToDto(),
            Roles = userPrincipal.FindFirstValue(ClaimTypes.Role)
        };
    }

    public async Task<(IdentityResult Result, AddressDto? Address)> CreateOrUpdateAddressAsync(ClaimsPrincipal userPrincipal, AddressDto addressDto)
    {
        var user = await userManager.Users.Include(x => x.Address).FirstOrDefaultAsync(x => x.Email == userPrincipal.FindFirstValue(ClaimTypes.Email));
        if (user == null) return (IdentityResult.Failed(new IdentityError { Description = "User not found" }), null);

        if (user.Address == null)
        {
            user.Address = addressDto.ToEntity();
        }
        else
        {
            user.Address.UpdateFromDto(addressDto);
        }

        var result = await userManager.UpdateAsync(user);
        
        return (result, result.Succeeded ? user.Address.ToDto() : null);
    }

    public async Task<IdentityResult> UpdateProfileAsync(ClaimsPrincipal userPrincipal, UpdateProfileDto profileDto)
    {
        var user = await userManager.FindByEmailAsync(userPrincipal.FindFirstValue(ClaimTypes.Email) ?? string.Empty);
        if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });

        user.FirstName = profileDto.FirstName;
        user.LastName = profileDto.LastName;
        user.PhoneNumber = profileDto.PhoneNumber;

        return await userManager.UpdateAsync(user);
    }

    public async Task<IdentityResult> UpdateLanguageAsync(ClaimsPrincipal userPrincipal, UpdateLanguageDto languageDto)
    {
        var user = await userManager.FindByEmailAsync(userPrincipal.FindFirstValue(ClaimTypes.Email) ?? string.Empty);
        if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });

        user.Language = languageDto.Language;

        return await userManager.UpdateAsync(user);
    }

    public async Task<IdentityResult> DeleteAccountAsync(ClaimsPrincipal userPrincipal)
    {
        var user = await userManager.FindByEmailAsync(userPrincipal.FindFirstValue(ClaimTypes.Email) ?? string.Empty);
        if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found" });

        var result = await userManager.DeleteAsync(user);

        if (result.Succeeded)
        {
            // No-op for JWT
        }

        return result;
    }
}
