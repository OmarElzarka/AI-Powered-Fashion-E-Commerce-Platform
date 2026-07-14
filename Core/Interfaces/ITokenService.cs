using System.Threading.Tasks;
using Core.Entities;

namespace Core.Interfaces;

public interface ITokenService
{
    Task<string> GenerateJwtToken(AppUser user);
    Task<RefreshToken> GenerateRefreshToken(AppUser user);
}
