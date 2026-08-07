using Application.Abstractions.Persistence;
using Application.Abstractions.Queue;
using Application.Interface;
using Core.Enums;
using Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace Infrastructure.BackgroundServices;


public sealed class SubmissionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public SubmissionBackgroundService(
        IServiceProvider serviceProvider,
        ILogger logger
        )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }


    protected override async Task ExecuteAsync(
        CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            using var scope =
                _serviceProvider.CreateScope();


    
            var operationRepository =
                scope.ServiceProvider
                    .GetRequiredService<IOperationRepository>();


            var queue =
                scope.ServiceProvider
                    .GetRequiredService<ISubmissionQueue>();



            

            var operations =
                    await operationRepository.GetReadyForRetryAsync(token);

            foreach(var operation in operations)
            {
                
                _logger.LogInformation(
                "Retrying operation {OperationId}",
                operation.Id);

                await queue.EnqueueAsync(
                    operation,
                    token);
            }



            await Task.Delay(
                TimeSpan.FromSeconds(5),
                token);
        }
    }
}