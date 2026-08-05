

using Application.Abstractions.Queue;
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


    // services.AddHttpClient<IProviderClient, HttpProviderClient>();

    //
    // services.AddHostedService<SubmissionProducerWorker>();
    //
    // services.AddHostedService<SubmissionConsumerWorker>();

    return services;
  }
}

