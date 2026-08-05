using Application.Abstractions.Persistence;
using Application.Abstractions.Providers;
using Application.Interface;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DependencyInjection
{
  public static class ProviderExtensions
  {

    public static IServiceCollection AddProviders(
     this IServiceCollection services)
    {
      services.AddScoped<IOperationRepository, OperationRepository>();
      services.AddSingleton<IProviderClientFactory,ProviderClientFactory>();
      services.AddScoped<IPaymentAttemptRepository,PaymentAttemptRepository>();
      services.AddScoped<IUnitOfWork, UnitOfWork>();
      return services;
    }
  }
}
