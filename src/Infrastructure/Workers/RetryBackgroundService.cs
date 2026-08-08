using Application.Abstractions.Persistence;
using Application.Abstractions.Queue;
using Application.Abstractions.Submission;
using Application.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundServices;

public sealed class RetryBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RetryBackgroundService> _logger;

    public RetryBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<RetryBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var scope =
                    _serviceProvider.CreateScope();

                var operationRepository =
                    scope.ServiceProvider
                        .GetRequiredService<IOperationRepository>();

                var submissionQueue =
                    scope.ServiceProvider
                        .GetRequiredService<ISubmissionQueue>();

                var operations =
                    await operationRepository
                        .GetReadyForRetryAsync(token);

                foreach (var operation in operations)
                {
                    await submissionQueue.EnqueueAsync(
                        operation,
                        token);

                    _logger.LogInformation(
                        "Operation {OperationId} scheduled for retry",
                        operation.Id);
                }
            }
            catch (OperationCanceledException)
                when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while processing operation retries");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(5),
                token);
        }
    }
}