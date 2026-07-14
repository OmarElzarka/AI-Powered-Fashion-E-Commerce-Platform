using System.Security.Claims;
using API.DTOs;
using API.Extensions;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController(SignInManager<AppUser> signInManager, ITokenService tokenService, StoreContext context) : BaseApiController
{
    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterDto registerDto)
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

        var result = await signInManager.UserManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem();
        }

        return Ok();
    }

    [HttpPost("login-jwt")]
    public async Task<ActionResult<TokenDto>> LoginJwt(LoginDto loginDto)
    {
        var user = await signInManager.UserManager.FindByEmailAsync(loginDto.Email);
        if (user == null) 
        {
            Console.WriteLine($"DEBUG: Login failed. User '{loginDto.Email}' not found.");
            return Unauthorized();
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
        if (!result.Succeeded) 
        {
            Console.WriteLine($"DEBUG: Login failed. CheckPasswordSignInAsync failed for user '{loginDto.Email}'. IsLockedOut={result.IsLockedOut}, IsNotAllowed={result.IsNotAllowed}");
            return Unauthorized();
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

    [HttpPost("refresh-token")]
    public async Task<ActionResult<TokenDto>> RefreshToken([FromBody] string token)
    {
        var refreshToken = await context.RefreshTokens
            .Include(x => x.AppUser)
            .FirstOrDefaultAsync(x => x.Token == token);

        if (refreshToken == null || !refreshToken.IsActive || refreshToken.AppUser == null)
            return Unauthorized();

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

    [HttpGet("check-email")]
    public async Task<ActionResult<bool>> CheckEmailExists([FromQuery] string email)
    {
        return await signInManager.UserManager.FindByEmailAsync(email) != null;
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("user-info")]
    public async Task<ActionResult> GetUserInfo()
    {
        var user = await signInManager.UserManager.GetUserByEmailWithAddress(User);
        if (user == null) return NoContent();

        return Ok(new
        {
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.Language,
            Address = user.Address?.ToDto(),
            Roles = User.FindFirstValue(ClaimTypes.Role)
        });
    }

    [HttpGet("auth-status")]
    public ActionResult GetAuthState()
    {
        return Ok(new
        {
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false
        });
    }
    
    [Authorize]
    [HttpPost("address")]
    public async Task<ActionResult<Address>> CreateOrUpdateAddress(AddressDto addressDto)
    {
        var user = await signInManager.UserManager.GetUserByEmailWithAddress(User);
        if (user == null) return Unauthorized();

        if (user.Address == null)
        {
            user.Address = addressDto.ToEntity();
        }
        else
        {
            user.Address.UpdateFromDto(addressDto);
        }

        var result = await signInManager.UserManager.UpdateAsync(user);

        if (!result.Succeeded) return BadRequest("Problem updating user address");

        return Ok(user.Address.ToDto());
    }

    [Authorize]
    [HttpPost("profile")]
    public async Task<ActionResult> UpdateProfile(UpdateProfileDto profileDto)
    {
        var user = await signInManager.UserManager.FindByEmailAsync(User.FindFirstValue(ClaimTypes.Email) ?? string.Empty);
        if (user == null) return Unauthorized();

        user.FirstName = profileDto.FirstName;
        user.LastName = profileDto.LastName;
        user.PhoneNumber = profileDto.PhoneNumber;

        var result = await signInManager.UserManager.UpdateAsync(user);

        if (!result.Succeeded) return BadRequest("Problem updating user profile");

        return Ok();
    }

    [Authorize]
    [HttpPost("language")]
    public async Task<ActionResult> UpdateLanguage(UpdateLanguageDto languageDto)
    {
        var user = await signInManager.UserManager.FindByEmailAsync(User.FindFirstValue(ClaimTypes.Email) ?? string.Empty);
        if (user == null) return Unauthorized();

        user.Language = languageDto.Language;

        var result = await signInManager.UserManager.UpdateAsync(user);

        if (!result.Succeeded) return BadRequest("Problem updating language preference");

        return Ok();
    }

    [Authorize]
    [HttpDelete]
    public async Task<ActionResult> DeleteAccount()
    {
        var user = await signInManager.UserManager.FindByEmailAsync(User.FindFirstValue(ClaimTypes.Email) ?? string.Empty);
        if (user == null) return Unauthorized();

        var result = await signInManager.UserManager.DeleteAsync(user);

        if (!result.Succeeded) return BadRequest("Problem deleting account");

        await signInManager.SignOutAsync();

        return Ok();
    }
}