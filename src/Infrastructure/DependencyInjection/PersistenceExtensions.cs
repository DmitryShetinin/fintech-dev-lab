using Infrastructure.Configuration;
using Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Application.Interface;
using Application.Abstractions.Persistence;
using Application.DomainEvents;
using Infrastructure.Persistence.Repositories;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {



        services.AddDbContext<AppDbContext>(
         (sp, options) =>
         {
             var dbOptions =
                 sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;

             options.UseNpgsql(
                 configuration.GetConnectionString("Default"),
                 npgsql =>
                 {
                     npgsql.CommandTimeout(
                         dbOptions.CommandTimeoutSeconds);


                 });
         });
        services.AddScoped<IOperationRepository, OperationRepository>();
        services.AddScoped<IPaymentAttemptRepository, PaymentAttemptRepository>();
        services.AddScoped<IOperationEventRepository, OperationEventRepository>();
        services.AddScoped<DomainEventDispatcher>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();


        return services;
    }
}
