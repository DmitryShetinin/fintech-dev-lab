
using Application.Abstractions.Telemetry;
using Infrastructure.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DependencyInjection;

public static class TelemetryExtensions
{

  public static IServiceCollection AddTelemetry(
   this IServiceCollection services)
  {
    services.AddSingleton<IOperationMetrics, OpenTelemetryOperationMetrics>();

    return services;
  }
}
