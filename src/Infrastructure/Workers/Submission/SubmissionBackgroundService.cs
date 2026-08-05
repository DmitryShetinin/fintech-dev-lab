using Application.Abstractions.Persistence;
using Application.Abstractions.Queue;
using Application.Interface;
using Core.Enums;
using Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace Infrastructure.BackgroundServices;


public sealed class SubmissionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;


    public SubmissionBackgroundService(
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


            var attemptRepository =
                scope.ServiceProvider
                    .GetRequiredService<IPaymentAttemptRepository>();


            var operationRepository =
                scope.ServiceProvider
                    .GetRequiredService<IOperationRepository>();


            var queue =
                scope.ServiceProvider
                    .GetRequiredService<ISubmissionQueue>();



            var attempts =
                await attemptRepository.GetReadyForRetryAsync(
                    PaymentAttemptType.Submission,
                    DateTime.UtcNow,
                    token);



            foreach(var attempt in attempts)
            {
                var operation =
                    await operationRepository.GetByIdAsync(
                        attempt.OperationId,
                        token);


                if (operation is null)
                    continue;


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