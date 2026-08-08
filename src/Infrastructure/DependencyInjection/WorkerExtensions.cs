

using Application.Abstractions.Queue;
using Infrastructure.BackgroundServices;
using Infrastructure.Configuration;
using Infrastructure.Queue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DependencyInjection;



public static class WorkerExtensions
{

  public static IServiceCollection AddWorkers(
   this IServiceCollection services, IConfiguration configuration)
  {
    services.Configure<WorkerOptions>(
        configuration.GetSection("Workers"));

    services.AddSingleton<ISubmissionQueue, SubmissionQueue>();
 

    services.AddHostedService<SubmissionBackgroundService>();
 
    services.AddHostedService<RetryBackgroundService>();
 

    return services;
  }
}

