namespace Application.Abstractions.Providers;

public interface IProviderClientFactory
{

  IProviderClient Get(PaymentProvider provider);
}
