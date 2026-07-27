
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Application.Abstractions.Submission;
using Application.Abstractions.Receipt;




// namespace Infrastructure.BackgroundServices;
//
//
// public class ReceiptBackgroundService : BackgroundService
// {
//   private readonly IServiceProvider _serviceProvider;
//
//   protected override async Task ExecuteAsync(
//       CancellationToken token)
//   {
//     while (!token.IsCancellationRequested)
//     {
//       using var scope = _serviceProvider.CreateScope();
//
//       var processor = scope.ServiceProvider
//           .GetRequiredService<IReceiptProcessor>();
//
//       await processor.ProcessAsync(token);
//
//       await Task.Delay(
//           TimeSpan.FromSeconds(1),
//           token);
//     }
//   }
// }
