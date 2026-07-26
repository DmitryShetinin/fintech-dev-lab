using Core.Enums;

namespace Application.Abstractions.Providers;

public interface IProviderClientFactory
{

  IProviderClient Get(PaymentProvider provider);
}
