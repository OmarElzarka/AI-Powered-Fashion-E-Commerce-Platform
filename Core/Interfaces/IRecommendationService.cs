using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Entities;

namespace Core.Interfaces;

public interface IRecommendationService
{
    Task<List<Product>> GetRecommendationsAsync(int productId, int limit = 5);
}
