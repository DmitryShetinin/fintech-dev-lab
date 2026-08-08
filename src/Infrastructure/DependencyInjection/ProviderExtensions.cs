using Application.Abstractions.Providers;
using Infrastructure.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DependencyInjection
{
  public static class ProviderExtensions
  {

    public static IServiceCollection AddProviders(
     this IServiceCollection services, 
     IConfiguration configuration)
    {
      services.AddScoped<ProviderFailureClassifier>();
      services.AddScoped<IProviderClientFactory,ProviderClientFactory>();
     

      services.AddHttpClient<YooKassaProvider>(client =>
      {
          client.BaseAddress =
              new Uri(configuration["PROVIDER_URL"]!);
      });
      return services;
    }
  }
}
