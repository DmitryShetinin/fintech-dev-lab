using Application.Abstractions.Queue;
using Application.Interface;
using Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;



namespace Infrastructure.BackgroundServices;


public sealed class ReceiptBackgroundService : BackgroundService
{

    private readonly IServiceProvider _serviceProvider;


    public ReceiptBackgroundService(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }



    protected override async Task ExecuteAsync(
        CancellationToken token)
    {

        while (!token.IsCancellationRequested)
        {

            using var scope =
                _serviceProvider.CreateScope();


            var repository =
                scope.ServiceProvider
                    .GetRequiredService<IOperationRepository>();


            var queue =
                scope.ServiceProvider
                    .GetRequiredService<IReceiptQueue>();


            var operations =
                await repository.GetWaitingForReceiptAsync(
                    token);



            foreach (var operation in operations)
            {
                await queue.EnqueueAsync(
                    operation,
                    token);
            }



            await Task.Delay(
                TimeSpan.FromSeconds(1),
                token);
        }
    }
}