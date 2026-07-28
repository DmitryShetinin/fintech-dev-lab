using Application.Interface;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DependencyInjection
{
  public static class ProviderExtensions
  {

    public static IServiceCollection AddProviders(
     this IServiceCollection services)
    {
      services.AddScoped<IOperationRepository, OperationRepository>();

      services.AddScoped<IUnitOfWork, UnitOfWork>();

      return services;
    }
  }
}
