using Application.Abstractions.Queue;
using Application.Abstractions.Submission;
using Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Workers.Submission;


public sealed class SubmissionWorker : BackgroundService
{
    private readonly ISubmissionQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly WorkerOptions _options;


    public SubmissionWorker(
        ISubmissionQueue queue,
        IServiceProvider serviceProvider,
        WorkerOptions options)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _options = options;
    }


    protected override async Task ExecuteAsync(
        CancellationToken token)
    {
        var tasks =
            Enumerable.Range(
                0,
                _options.SubmissionWorkers)
            .Select(_ => ConsumeAsync(token));


        await Task.WhenAll(tasks);
    }


    private async Task ConsumeAsync(
        CancellationToken token)
    {
        await foreach(var operationId in _queue.ReadAllAsync(token))
        {
            using var scope =
                _serviceProvider.CreateScope();


            var processor =
                scope.ServiceProvider
                    .GetRequiredService<ISubmissionProcessor>();


            await processor.SubmitOperationAsync(
                operationId,
                token);
        }
    }
}