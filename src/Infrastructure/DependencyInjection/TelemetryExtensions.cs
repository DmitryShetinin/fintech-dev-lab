
using Application.Abstractions.Telemetry;
using Infrastructure.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;

namespace Infrastructure.DependencyInjection;

public static class TelemetryExtensions
{

  public static IServiceCollection AddTelemetry(
   this IServiceCollection services)
  {
    services.AddSingleton<IOperationMetrics, OpenTelemetryOperationMetrics>();
    services
    .AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
      metrics
          .AddMeter("FintechDevLab")
          .AddPrometheusExporter();
    });
    return services;
  }
}
