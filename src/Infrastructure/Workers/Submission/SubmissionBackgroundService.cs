using Application.Abstractions.Queue;
using Application.Abstractions.Submission;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundServices;

public sealed class SubmissionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISubmissionQueue _queue;
    private readonly ILogger<SubmissionBackgroundService> _logger;

    public SubmissionBackgroundService(
        IServiceProvider serviceProvider,
        ISubmissionQueue queue,
        ILogger<SubmissionBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var operation =
                    await _queue.DequeueAsync(token);

                using var scope =
                    _serviceProvider.CreateScope();

                var processor =
                    scope.ServiceProvider
                        .GetRequiredService<ISubmissionProcessor>();

                _logger.LogInformation(
                    "Processing submission for operation {OperationId}",
                    operation.Id);

                await processor.SubmitOperationAsync(
                    operation,
                    token);
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
                    "Error while processing submission");
            }
        }
    }
}