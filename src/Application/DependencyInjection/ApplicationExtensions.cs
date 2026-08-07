using Application.Abstractions.Retry;
using Application.Abstractions.Submission;
using Application.Operations;
using Application.Receipts;
using Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application;

public static class DependencyInjection
{
  public static IServiceCollection AddApplication(
      this IServiceCollection services)
  {
    services.AddSingleton<OperationStateMachine>();
    services.AddLogging();
    services.AddScoped<IOperationService, OperationService>();
    services.AddScoped<IReceiptService, ReceiptService>();
    services.AddSingleton<IRetryPolicy,ExponentialBackoffRetryPolicy>();
    services.AddScoped<ISubmissionProcessor, SubmissionProcessor>();

    return services;
  }
}
