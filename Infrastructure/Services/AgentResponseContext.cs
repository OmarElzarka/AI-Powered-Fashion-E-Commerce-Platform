using System.Collections.Generic;
using Core.Entities;

namespace Infrastructure.Services;

public class AgentResponseContext
{
    public List<Product> ProductsToDisplay { get; } = new();
    public ActionConfirmation? PendingConfirmation { get; set; }
}
