using Application.Abstractions.Queue;
using Application.Abstractions.Receipt;
using Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace Infrastructure.BackgroundServices;


public sealed class ReceiptWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IReceiptQueue _queue;
    private readonly WorkerOptions _options;


    public ReceiptWorker(
        IServiceProvider serviceProvider,
        IReceiptQueue queue,
        WorkerOptions options)
    {
        _serviceProvider = serviceProvider;
        _queue = queue;
        _options = options;
    }


    protected override async Task ExecuteAsync(
        CancellationToken token)
    {
        var tasks =
            Enumerable.Range(
                0,
                _options.ReceiptWorkers)
            .Select(_ => ConsumeAsync(token));


        await Task.WhenAll(tasks);
    }



    private async Task ConsumeAsync(
        CancellationToken token)
    {
        await foreach(var operation in _queue.ReadAllAsync(token))
        {
            using var scope =
                _serviceProvider.CreateScope();


            var processor =
                scope.ServiceProvider
                    .GetRequiredService<IReceiptProcessor>();


            await processor.ProcessAsync(
                operation,
                token);
        }
    }
}