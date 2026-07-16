using System.Security.Claims;
using Core.DTOs;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class AccountController(IAccountService accountService) : BaseApiController
{
    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterDto registerDto)
    {
        var result = await accountService.RegisterAsync(registerDto);

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
        var result = await accountService.LoginJwtAsync(loginDto);
        
        if (result == null) return Unauthorized();

        return Ok(result);
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<TokenDto>> RefreshToken([FromBody] string token)
    {
        var result = await accountService.RefreshTokenAsync(token);

        if (result == null) return Unauthorized();

        return Ok(result);
    }

    [HttpGet("check-email")]
    public async Task<ActionResult<bool>> CheckEmailExists([FromQuery] string email)
    {
        return Ok(await accountService.CheckEmailExistsAsync(email));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult> Logout()
    {
        await accountService.LogoutAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("user-info")]
    public async Task<ActionResult> GetUserInfo()
    {
        var userInfo = await accountService.GetUserInfoAsync(User);
        
        if (userInfo == null) return NoContent();

        return Ok(userInfo);
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
    public async Task<ActionResult<AddressDto>> CreateOrUpdateAddress(AddressDto addressDto)
    {
        var (result, address) = await accountService.CreateOrUpdateAddressAsync(User, addressDto);

        if (!result.Succeeded) return BadRequest("Problem updating user address");

        return Ok(address);
    }

    [Authorize]
    [HttpPost("profile")]
    public async Task<ActionResult> UpdateProfile(UpdateProfileDto profileDto)
    {
        var result = await accountService.UpdateProfileAsync(User, profileDto);

        if (!result.Succeeded) return BadRequest("Problem updating user profile");

        return Ok();
    }

    [Authorize]
    [HttpPost("language")]
    public async Task<ActionResult> UpdateLanguage(UpdateLanguageDto languageDto)
    {
        var result = await accountService.UpdateLanguageAsync(User, languageDto);

        if (!result.Succeeded) return BadRequest("Problem updating language preference");

        return Ok();
    }

    [Authorize]
    [HttpDelete]
    public async Task<ActionResult> DeleteAccount()
    {
        var result = await accountService.DeleteAccountAsync(User);

        if (!result.Succeeded) return BadRequest("Problem deleting account");

        return Ok();
    }
}
