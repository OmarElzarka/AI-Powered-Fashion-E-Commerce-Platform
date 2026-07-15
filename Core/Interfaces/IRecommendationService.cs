using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Entities;

namespace Core.Interfaces;

public interface IRecommendationService
{
    Task<List<Product>> GetRecommendationsAsync(int productId, int limit = 5);
    Task<List<Product>> SearchByVectorAsync(float[] queryVector, int limit = 5);
}
