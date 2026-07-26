using Application.Abstractions.Providers;
using Microsoft.Extensions.DependencyInjection;

using Core.Enums;

namespace Infrastructure.Providers;

public sealed class ProviderClientFactory : IProviderClientFactory
{
  private readonly IServiceProvider _serviceProvider;

  public ProviderClientFactory(
      IServiceProvider serviceProvider)
  {
    _serviceProvider = serviceProvider;
  }

  public IProviderClient Get(
      PaymentProvider provider)
  {
    return provider switch
    {
      PaymentProvider.YooKassa =>
          _serviceProvider.GetRequiredService<YooKassaProvider>(),

      _ => throw new NotSupportedException()
    };
  }
}
