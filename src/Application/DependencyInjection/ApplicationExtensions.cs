using Application.Abstractions.Submission;
using Application.Operations;
using Application.Receipts;
using Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
  public static IServiceCollection AddApplication(
      this IServiceCollection services)
  {
    services.AddSingleton<OperationStateMachine>();

    services.AddScoped<IOperationService, OperationService>();
    services.AddScoped<IReceiptService, ReceiptService>();

    services.AddScoped<ISubmissionProcessor, SubmissionProcessor>();

    return services;
  }
}
