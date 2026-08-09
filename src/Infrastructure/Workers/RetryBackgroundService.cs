using Application.Abstractions.Persistence;
using Application.Abstractions.Queue;
using Application.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundServices;

public sealed class RetryBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RetryBackgroundService> _logger;

    private static readonly TimeSpan RetryCheckInterval =
        TimeSpan.FromSeconds(5);

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
        _logger.LogInformation(
            "Retry background service started");

        // Сначала пытаемся восстановить незавершённые операции.
        await RecoverProcessingOperationsAsync(token);

        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(
                    RetryCheckInterval,
                    token);

                await ProcessReadyRetriesAsync(token);
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
        }

        _logger.LogInformation(
            "Retry background service stopped");
    }

    private async Task ProcessReadyRetriesAsync(
        CancellationToken token)
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

    private async Task RecoverProcessingOperationsAsync(
    CancellationToken token)
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
                .GetProcessingOperationsAsync(token);

        foreach (var operation in operations)
        {
            await submissionQueue.EnqueueAsync(
                operation,
                token);

            _logger.LogInformation(
                "Recovered processing operation {OperationId}",
                operation.Id);
        }
    }

}